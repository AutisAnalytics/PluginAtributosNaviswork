using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;

namespace AutisAnalytics.NavisworksAtributos
{
    internal enum MatchStatus
    {
        Matched,
        Candidate,
        New,
        Removed,
        Conflict
    }

    internal enum MergeDepth
    {
        Automatic,   // Levels 1-3
        Deep,        // Levels 1-4 (future)
        Ultra        // Levels 1-5 with AI (future)
    }

    internal class MergeConfig
    {
        public string NewNwdPath { get; set; }
        public bool TransferAttributes { get; set; } = true;
        public bool TransferSets { get; set; } = true;
        public MergeDepth Depth { get; set; } = MergeDepth.Automatic;
        public string PreferredIdProperty { get; set; }
        public double AutoMatchThreshold { get; set; } = 80.0;
        public double CandidateThreshold { get; set; } = 60.0;
    }

    internal class ElementFingerprint
    {
        // Reference
        public ModelItem Item { get; set; }
        public string ModelSource { get; set; }

        // Unique IDs (Level 1)
        public string IfcGUID { get; set; }
        public string ElementId { get; set; }
        public string UniqueId { get; set; }
        public string SourceGuid { get; set; }
        public string ItemGuid { get; set; }

        // Basic identification (Level 2)
        public string DisplayName { get; set; }
        public string ClassName { get; set; }
        public string TypeName { get; set; }
        public string CategoryName { get; set; }

        // Geometry (Level 3)
        public double? BBoxCenterX { get; set; }
        public double? BBoxCenterY { get; set; }
        public double? BBoxCenterZ { get; set; }
        public string HierarchyPath { get; set; }

        // Autis data to transfer
        public List<AtributoCustom> AutisAttributes { get; set; }
        public List<string> AutisSets { get; set; }

        // Match result (populated during matching)
        public MatchResult Match { get; set; }

        public bool HasBBox => BBoxCenterX.HasValue && BBoxCenterY.HasValue && BBoxCenterZ.HasValue;

        public bool HasAutisData =>
            (AutisAttributes != null && AutisAttributes.Count > 0) ||
            (AutisSets != null && AutisSets.Count > 0);

        public string GetId(string fieldName)
        {
            switch (fieldName)
            {
                case "IfcGUID":    return IfcGUID;
                case "ElementId":  return ElementId;
                case "UniqueId":   return UniqueId;
                case "SourceGuid": return SourceGuid;
                case "ItemGuid":   return ItemGuid;
                default:           return null;
            }
        }

        public static readonly string[] IdFieldNames =
            { "IfcGUID", "ElementId", "UniqueId", "SourceGuid", "ItemGuid" };
    }

    internal class MatchResult
    {
        public MatchStatus Status { get; set; }
        public int Level { get; set; }
        public double Score { get; set; }
        public ElementFingerprint Partner { get; set; }
        public string Justification { get; set; }

        public MatchResult(MatchStatus status, int level, double score, string justification)
        {
            Status = status;
            Level = level;
            Score = score;
            Justification = justification;
        }
    }

    internal class MergeReport
    {
        public List<ElementFingerprint> OldFingerprints { get; set; } = new List<ElementFingerprint>();
        public List<ElementFingerprint> NewFingerprints { get; set; } = new List<ElementFingerprint>();

        public List<(ElementFingerprint Old, ElementFingerprint New, MatchResult Result)> Matched { get; set; }
            = new List<(ElementFingerprint, ElementFingerprint, MatchResult)>();

        public List<ElementFingerprint> NewElements { get; set; } = new List<ElementFingerprint>();
        public List<ElementFingerprint> RemovedElements { get; set; } = new List<ElementFingerprint>();

        public List<(ElementFingerprint Old, ElementFingerprint New, MatchResult Result)> Candidates { get; set; }
            = new List<(ElementFingerprint, ElementFingerprint, MatchResult)>();

        public TimeSpan AnalysisDuration { get; set; }

        public int TotalAttributesToTransfer =>
            Matched.Sum(m => m.Old.AutisAttributes?.Count ?? 0);

        public int TotalSetsToReconstruct =>
            Matched.Where(m => m.Old.AutisSets != null)
                   .SelectMany(m => m.Old.AutisSets)
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .Count();

        public int MatchedLevel1 => Matched.Count(m => m.Result.Level == 1);
        public int MatchedLevel2 => Matched.Count(m => m.Result.Level == 2);
        public int MatchedLevel3 => Matched.Count(m => m.Result.Level == 3);
    }
}

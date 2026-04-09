using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Navisworks.Api;

namespace AutisAnalytics.NavisworksAtributos
{
    internal static class MergeService
    {
        // Known ID property locations: (CategoryDisplayName, PropertyDisplayName) → field name
        private static readonly (string Cat, string Prop, string Field)[] ID_SOURCES =
        {
            ("Element",     "IfcGUID",    "IfcGUID"),
            ("Element",     "GUID",       "IfcGUID"),
            ("Element",     "Element ID", "ElementId"),
            ("Element",     "ElementId",  "ElementId"),
            ("Element",     "UniqueId",   "UniqueId"),
            ("LcOaNode",    "SourceGuid", "SourceGuid"),
            ("LcOaNode",    "Guid",       "SourceGuid"),
            ("Item",        "GUID",       "ItemGuid"),
            ("Item",        "Id",         "ItemGuid"),
            ("LcRevitData", "ElementId",  "ElementId"),
            ("Revit",       "ElementId",  "ElementId"),
            // IFC common
            ("IFC",         "GUID",       "IfcGUID"),
            ("IFC",         "IfcGUID",    "IfcGUID"),
        };

        // Type/Category property locations
        private static readonly (string Cat, string Prop)[] TYPE_SOURCES =
        {
            ("Element", "Type"),
            ("Element", "Type Name"),
            ("Type",    "Name"),
            ("Type",    "Type Name"),
            ("LcRevitData", "Type"),
            ("IFC",     "Type"),
        };

        private static readonly (string Cat, string Prop)[] CATEGORY_SOURCES =
        {
            ("Element", "Category"),
            ("Item",    "Category"),
            ("IFC",     "ObjectType"),
        };

        // ─────────────────────────────────────────────────────────────────
        // FINGERPRINT EXTRACTION
        // ─────────────────────────────────────────────────────────────────

        public static List<ElementFingerprint> ExtractFingerprints(
            IEnumerable<ModelItem> rootItems,
            string modelSource,
            bool readAutisData,
            Action<string, int> progress = null)
        {
            var result = new List<ElementFingerprint>();

            // First pass: count total elements for progress
            int totalEstimate = 0;
            foreach (var root in rootItems)
            {
                try { totalEstimate += root.DescendantsAndSelf.Count(); }
                catch { totalEstimate += 10000; }
            }
            if (totalEstimate == 0) totalEstimate = 1;

            int count = 0;
            foreach (var root in rootItems)
            {
                foreach (var item in root.DescendantsAndSelf)
                {
                    // Skip group/container nodes without properties
                    if (item.PropertyCategories.Count() == 0)
                        continue;

                    var fp = ExtractSingleFingerprint(item, modelSource, readAutisData);
                    if (fp != null)
                        result.Add(fp);

                    count++;
                    if (count % 200 == 0)
                    {
                        int pct = Math.Min(100, (int)((double)count / totalEstimate * 100));
                        progress?.Invoke(
                            $"Extracting {modelSource}: {count:N0}/{totalEstimate:N0} elements...",
                            pct);
                    }
                }
            }

            progress?.Invoke($"Extracted {result.Count:N0} fingerprints from {modelSource}", 100);
            return result;
        }

        private static ElementFingerprint ExtractSingleFingerprint(
            ModelItem item, string modelSource, bool readAutisData)
        {
            var fp = new ElementFingerprint
            {
                Item = item,
                ModelSource = modelSource,
                DisplayName = item.DisplayName ?? "",
                ClassName = item.ClassDisplayName ?? "",
            };

            // Read properties for IDs, type, category
            foreach (var cat in item.PropertyCategories)
            {
                var catName = cat.DisplayName ?? cat.Name ?? "";

                foreach (var prop in cat.Properties)
                {
                    var propName = prop.DisplayName ?? prop.Name ?? "";
                    var value = FormatValue(prop.Value);

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    // Check ID sources
                    foreach (var src in ID_SOURCES)
                    {
                        if (string.Equals(catName, src.Cat, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(propName, src.Prop, StringComparison.OrdinalIgnoreCase))
                        {
                            SetIdField(fp, src.Field, value);
                        }
                    }

                    // Check type sources
                    if (string.IsNullOrEmpty(fp.TypeName))
                    {
                        foreach (var src in TYPE_SOURCES)
                        {
                            if (string.Equals(catName, src.Cat, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(propName, src.Prop, StringComparison.OrdinalIgnoreCase))
                            {
                                fp.TypeName = value;
                                break;
                            }
                        }
                    }

                    // Check category sources
                    if (string.IsNullOrEmpty(fp.CategoryName))
                    {
                        foreach (var src in CATEGORY_SOURCES)
                        {
                            if (string.Equals(catName, src.Cat, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(propName, src.Prop, StringComparison.OrdinalIgnoreCase))
                            {
                                fp.CategoryName = value;
                                break;
                            }
                        }
                    }
                }
            }

            // Skip elements with absolutely no identification
            bool hasAnyId = !string.IsNullOrEmpty(fp.IfcGUID) ||
                            !string.IsNullOrEmpty(fp.ElementId) ||
                            !string.IsNullOrEmpty(fp.UniqueId) ||
                            !string.IsNullOrEmpty(fp.SourceGuid) ||
                            !string.IsNullOrEmpty(fp.ItemGuid);
            bool hasName = !string.IsNullOrEmpty(fp.DisplayName);

            if (!hasAnyId && !hasName)
                return null;

            // BoundingBox
            try
            {
                var bbox = item.BoundingBox();
                if (bbox != null && !bbox.IsEmpty)
                {
                    fp.BBoxCenterX = (bbox.Min.X + bbox.Max.X) / 2.0;
                    fp.BBoxCenterY = (bbox.Min.Y + bbox.Max.Y) / 2.0;
                    fp.BBoxCenterZ = (bbox.Min.Z + bbox.Max.Z) / 2.0;
                }
            }
            catch { /* Some items may not have geometry */ }

            // Hierarchy path
            try
            {
                var ancestors = new List<string>();
                foreach (var ancestor in item.AncestorsAndSelf)
                {
                    if (!string.IsNullOrWhiteSpace(ancestor.DisplayName))
                        ancestors.Add(ancestor.DisplayName);
                }
                ancestors.Reverse();
                fp.HierarchyPath = string.Join(" > ", ancestors);
            }
            catch { fp.HierarchyPath = fp.DisplayName; }

            // Autis data (only for old model)
            if (readAutisData)
            {
                fp.AutisAttributes = new List<AtributoCustom>();
                fp.AutisSets = new List<string>();

                foreach (var cat in item.PropertyCategories)
                {
                    var catName = cat.DisplayName ?? cat.Name ?? "";
                    bool isAutisCategory =
                        string.Equals(catName, AutisSchema.CategoriaPrincipal, StringComparison.OrdinalIgnoreCase) ||
                        AutisSchema.CategoriasLegadas.Any(leg =>
                            string.Equals(catName, leg, StringComparison.OrdinalIgnoreCase));

                    if (!isAutisCategory) continue;

                    foreach (var prop in cat.Properties)
                    {
                        var propName = (prop.DisplayName ?? prop.Name ?? "").Trim();
                        var valor = FormatValue(prop.Value)?.Trim() ?? "";

                        if (IsSetProperty(propName))
                        {
                            foreach (var setName in SplitSets(valor))
                                fp.AutisSets.Add(setName);
                        }
                        else
                        {
                            fp.AutisAttributes.Add(new AtributoCustom(
                                AutisSchema.CategoriaPrincipal,
                                propName, valor,
                                GetValueType(prop.Value)));
                        }
                    }
                }

                fp.AutisSets = fp.AutisSets.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            return fp;
        }

        // ─────────────────────────────────────────────────────────────────
        // ID DETECTION
        // ─────────────────────────────────────────────────────────────────

        public static string DetectPreferredId(List<ElementFingerprint> fingerprints)
        {
            if (fingerprints == null || fingerprints.Count == 0)
                return null;

            var sample = fingerprints.Count <= 100
                ? fingerprints
                : fingerprints.Where((_, i) => i % (fingerprints.Count / 100) == 0)
                              .Take(100).ToList();

            string bestField = null;
            double bestScore = -1;

            foreach (var fieldName in ElementFingerprint.IdFieldNames)
            {
                var values = sample
                    .Select(fp => fp.GetId(fieldName))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                if (values.Count == 0) continue;

                double coverage = (double)values.Count / sample.Count;
                double uniqueness = (double)values.Distinct(StringComparer.OrdinalIgnoreCase).Count() / values.Count;
                double score = coverage * uniqueness;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestField = fieldName;
                }
            }

            return bestField;
        }

        // ─────────────────────────────────────────────────────────────────
        // MATCHING ORCHESTRATOR
        // ─────────────────────────────────────────────────────────────────

        public static MergeReport RunMatching(
            List<ElementFingerprint> oldFPs,
            List<ElementFingerprint> newFPs,
            MergeConfig config,
            Action<string, int> progress = null)
        {
            var sw = Stopwatch.StartNew();
            var report = new MergeReport
            {
                OldFingerprints = oldFPs,
                NewFingerprints = newFPs
            };

            var unmatchedOld = new List<ElementFingerprint>(oldFPs);
            var unmatchedNew = new List<ElementFingerprint>(newFPs);

            // Level 1: Exact ID match (0-30%)
            progress?.Invoke("Level 1: Matching by unique ID...", 0);
            var matches1 = MatchLevel1(unmatchedOld, unmatchedNew, config.PreferredIdProperty);
            report.Matched.AddRange(matches1);
            RemoveMatched(unmatchedOld, unmatchedNew, matches1);
            progress?.Invoke($"Level 1: {matches1.Count:N0} matches found", 30);

            // Level 2: Composite key match (30-55%)
            progress?.Invoke("Level 2: Matching by composite key...", 35);
            var matches2 = MatchLevel2(unmatchedOld, unmatchedNew);
            report.Matched.AddRange(matches2);
            RemoveMatched(unmatchedOld, unmatchedNew, matches2);
            progress?.Invoke($"Level 2: {matches2.Count:N0} matches found", 55);

            // Level 3: Weighted scoring (55-90%)
            if (config.Depth >= MergeDepth.Automatic)
            {
                progress?.Invoke("Level 3: Scoring by properties and geometry...", 60);
                var (matches3, candidates3) = MatchLevel3(
                    unmatchedOld, unmatchedNew,
                    config.AutoMatchThreshold, config.CandidateThreshold);
                report.Matched.AddRange(matches3);
                report.Candidates.AddRange(candidates3);
                RemoveMatched(unmatchedOld, unmatchedNew, matches3);
                RemoveCandidates(unmatchedOld, unmatchedNew, candidates3);
                progress?.Invoke($"Level 3: {matches3.Count:N0} matches, {candidates3.Count:N0} candidates", 90);
            }

            // Remaining: New and Removed (90-100%)
            progress?.Invoke("Classifying remaining elements...", 92);
            foreach (var fp in unmatchedNew)
            {
                fp.Match = new MatchResult(MatchStatus.New, 0, 0, "No match in old model");
                report.NewElements.Add(fp);
            }
            foreach (var fp in unmatchedOld)
            {
                fp.Match = new MatchResult(MatchStatus.Removed, 0, 0, "No match in new model");
                report.RemovedElements.Add(fp);
            }

            sw.Stop();
            report.AnalysisDuration = sw.Elapsed;

            progress?.Invoke($"Analysis complete in {sw.Elapsed.TotalSeconds:F1}s", 100);
            return report;
        }

        // ─────────────────────────────────────────────────────────────────
        // LEVEL 1 — Exact ID match
        // ─────────────────────────────────────────────────────────────────

        private static List<(ElementFingerprint Old, ElementFingerprint New, MatchResult Result)>
            MatchLevel1(List<ElementFingerprint> oldFPs, List<ElementFingerprint> newFPs, string preferredId)
        {
            var result = new List<(ElementFingerprint, ElementFingerprint, MatchResult)>();
            var matchedOld = new HashSet<ElementFingerprint>();
            var matchedNew = new HashSet<ElementFingerprint>();

            var fieldOrder = preferredId != null
                ? new[] { preferredId }.Concat(
                    ElementFingerprint.IdFieldNames.Where(f => f != preferredId)).ToArray()
                : ElementFingerprint.IdFieldNames;

            foreach (var field in fieldOrder)
            {
                var oldLookup = BuildLookup(
                    oldFPs.Where(fp => !matchedOld.Contains(fp)),
                    fp => NormalizeId(fp.GetId(field)));
                var newLookup = BuildLookup(
                    newFPs.Where(fp => !matchedNew.Contains(fp)),
                    fp => NormalizeId(fp.GetId(field)));

                foreach (var pair in oldLookup)
                {
                    if (pair.Value.Count != 1) continue;
                    if (!newLookup.TryGetValue(pair.Key, out var candidates) || candidates.Count != 1)
                        continue;

                    var oldFP = pair.Value[0];
                    var newFP = candidates[0];
                    if (matchedOld.Contains(oldFP) || matchedNew.Contains(newFP))
                        continue;

                    var match = new MatchResult(MatchStatus.Matched, 1, 100,
                        $"Exact unique match on {field}: {pair.Key}");
                    oldFP.Match = match;
                    newFP.Match = match;
                    result.Add((oldFP, newFP, match));
                    matchedOld.Add(oldFP);
                    matchedNew.Add(newFP);
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // LEVEL 2 — Composite key match
        // ─────────────────────────────────────────────────────────────────

        private static List<(ElementFingerprint Old, ElementFingerprint New, MatchResult Result)>
            MatchLevel2(List<ElementFingerprint> oldFPs, List<ElementFingerprint> newFPs)
        {
            var result = new List<(ElementFingerprint, ElementFingerprint, MatchResult)>();
            var matchedOld = new HashSet<ElementFingerprint>();
            var matchedNew = new HashSet<ElementFingerprint>();

            var keyBuilders = new (Func<ElementFingerprint, string> Build, double Score, string Desc)[]
            {
                (fp => CompositeKey(fp.ElementId, fp.DisplayName), 95, "ElementId+Name"),
                (fp => CompositeKey(fp.TypeName, fp.DisplayName),  92, "Type+Name"),
                (fp => CompositeKey(fp.CategoryName, fp.DisplayName), 90, "Category+Name"),
                (fp => CompositeKey(fp.ClassName, fp.DisplayName),    88, "Class+Name"),
            };

            foreach (var kb in keyBuilders)
            {
                var oldLookup = BuildLookup(
                    oldFPs.Where(fp => !matchedOld.Contains(fp)),
                    kb.Build);
                var newLookup = BuildLookup(
                    newFPs.Where(fp => !matchedNew.Contains(fp)),
                    kb.Build);

                foreach (var pair in oldLookup)
                {
                    if (pair.Value.Count != 1) continue;
                    if (!newLookup.TryGetValue(pair.Key, out var candidates) || candidates.Count != 1)
                        continue;

                    var oldFP = pair.Value[0];
                    var newFP = candidates[0];
                    if (matchedOld.Contains(oldFP) || matchedNew.Contains(newFP))
                        continue;

                    var match = new MatchResult(MatchStatus.Matched, 2, kb.Score,
                        $"Composite unique match: {kb.Desc}");
                    oldFP.Match = match;
                    newFP.Match = match;
                    result.Add((oldFP, newFP, match));
                    matchedOld.Add(oldFP);
                    matchedNew.Add(newFP);
                }
            }

            return result;
        }

        private static string CompositeKey(string a, string b)
        {
            var left = NormalizeText(a);
            var right = NormalizeText(b);
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return null;
            return left + "|" + right;
        }

        // ─────────────────────────────────────────────────────────────────
        // LEVEL 3 — Weighted scoring
        // ─────────────────────────────────────────────────────────────────

        private static (
            List<(ElementFingerprint Old, ElementFingerprint New, MatchResult Result)> Matches,
            List<(ElementFingerprint Old, ElementFingerprint New, MatchResult Result)> Candidates)
            MatchLevel3(
                List<ElementFingerprint> oldFPs,
                List<ElementFingerprint> newFPs,
                double autoThreshold,
                double candidateThreshold)
        {
            var matches = new List<(ElementFingerprint, ElementFingerprint, MatchResult)>();
            var candidates = new List<(ElementFingerprint, ElementFingerprint, MatchResult)>();
            var matchedNew = new HashSet<ElementFingerprint>();

            var newByType = newFPs.GroupBy(fp => NormalizeText(fp.TypeName) ?? "")
                                  .ToDictionary(g => g.Key, g => g.ToList(),
                                                StringComparer.OrdinalIgnoreCase);
            var newByCategory = newFPs.GroupBy(fp => NormalizeText(fp.CategoryName) ?? "")
                                      .ToDictionary(g => g.Key, g => g.ToList(),
                                                    StringComparer.OrdinalIgnoreCase);
            var newByClass = newFPs.GroupBy(fp => fp.ClassName ?? "")
                                   .ToDictionary(g => g.Key, g => g.ToList(),
                                                 StringComparer.OrdinalIgnoreCase);

            foreach (var oldFP in oldFPs)
            {
                var searchPool = new List<ElementFingerprint>();
                var seen = new HashSet<ElementFingerprint>();
                AddCandidates(searchPool, seen, FindCandidates(newByType, oldFP.TypeName, matchedNew));
                AddCandidates(searchPool, seen, FindCandidates(newByCategory, oldFP.CategoryName, matchedNew));

                var className = oldFP.ClassName ?? "";
                if (newByClass.TryGetValue(className, out var sameClass))
                    AddCandidates(searchPool, seen, sameClass.Where(fp => !matchedNew.Contains(fp)));
                if (className != "" && newByClass.TryGetValue("", out var noClass))
                    AddCandidates(searchPool, seen, noClass.Where(fp => !matchedNew.Contains(fp)));
                if (searchPool.Count == 0)
                    AddCandidates(searchPool, seen, newFPs.Where(fp => !matchedNew.Contains(fp)));

                // Cap search pool to prevent O(n^2) explosion
                if (searchPool.Count > 500)
                {
                    searchPool = searchPool
                        .OrderByDescending(fp => QuickSearchScore(oldFP, fp))
                        .Take(500)
                        .ToList();
                }

                if (searchPool.Count == 0) continue;

                // Score each candidate
                var scored = new List<(ElementFingerprint New, double Score, string Detail)>();
                foreach (var newFP in searchPool)
                {
                    var (score, detail) = ComputeLevel3Score(oldFP, newFP);
                    if (score >= candidateThreshold)
                        scored.Add((newFP, score, detail));
                }

                if (scored.Count == 0) continue;

                scored.Sort((a, b) => b.Score.CompareTo(a.Score));
                var best = scored[0];

                // Conflict detection: two candidates very close in score
                if (scored.Count >= 2 && (scored[0].Score - scored[1].Score) < 5)
                {
                    var match = new MatchResult(MatchStatus.Conflict, 3, best.Score,
                        $"Conflict: top scores {scored[0].Score:F0} vs {scored[1].Score:F0}. {best.Detail}");
                    oldFP.Match = match;
                    candidates.Add((oldFP, best.New, match));
                    continue;
                }

                if (best.Score >= autoThreshold)
                {
                    var match = new MatchResult(MatchStatus.Matched, 3, best.Score, best.Detail);
                    oldFP.Match = match;
                    best.New.Match = match;
                    matches.Add((oldFP, best.New, match));
                    matchedNew.Add(best.New);
                }
                else
                {
                    var match = new MatchResult(MatchStatus.Candidate, 3, best.Score, best.Detail);
                    oldFP.Match = match;
                    candidates.Add((oldFP, best.New, match));
                }
            }

            return (matches, candidates);
        }

        private static (double Score, string Detail) ComputeLevel3Score(
            ElementFingerprint oldFP, ElementFingerprint newFP)
        {
            var parts = new List<string>();
            double total = 0;

            // Name similarity (30%)
            double nameRatio = LevenshteinRatio(oldFP.DisplayName, newFP.DisplayName);
            double nameScore = nameRatio * 30;
            total += nameScore;
            if (nameRatio > 0)
                parts.Add($"Name:{nameRatio:P0}");

            // Type / category match (25%)
            bool typeMatch = !string.IsNullOrEmpty(oldFP.TypeName) &&
                             string.Equals(oldFP.TypeName, newFP.TypeName, StringComparison.OrdinalIgnoreCase);
            bool categoryMatch = !string.IsNullOrEmpty(oldFP.CategoryName) &&
                                 string.Equals(oldFP.CategoryName, newFP.CategoryName, StringComparison.OrdinalIgnoreCase);
            bool classMatch = !string.IsNullOrEmpty(oldFP.ClassName) &&
                              string.Equals(oldFP.ClassName, newFP.ClassName, StringComparison.OrdinalIgnoreCase);

            if (typeMatch)
            {
                total += 15;
                parts.Add("Type:OK");
            }
            if (categoryMatch)
            {
                total += 10;
                parts.Add("Category:OK");
            }
            else if (classMatch)
            {
                total += 8;
                parts.Add("Class:OK");
            }

            // BBox proximity (25%)
            if (oldFP.HasBBox && newFP.HasBBox)
            {
                double dist = BBoxCenterDistance(oldFP, newFP);
                double geoScore = Math.Max(0, 25 * (1 - dist / 2.0));
                total += geoScore;
                if (geoScore > 0)
                    parts.Add($"Geo:{dist:F2}m");
            }

            // Hierarchy overlap (20%)
            double hierOverlap = HierarchyOverlap(oldFP.HierarchyPath, newFP.HierarchyPath);
            double hierScore = hierOverlap * 20;
            total += hierScore;
            if (hierOverlap > 0)
                parts.Add($"Hier:{hierOverlap:P0}");

            return (total, string.Join(" | ", parts));
        }

        // ─────────────────────────────────────────────────────────────────
        // MERGE EXECUTION
        // ─────────────────────────────────────────────────────────────────

        public static (int transferred, int errors, string message) ExecuteMerge(
            MergeReport report,
            MergeConfig config,
            Action<string, int> progress = null)
        {
            var toTransfer = report.Matched
                .Where(m => m.Old.HasAutisData)
                .ToList();

            // Add accepted candidates
            toTransfer.AddRange(
                report.Candidates
                    .Where(c => c.Result.Status == MatchStatus.Matched && c.Old.HasAutisData));

            if (toTransfer.Count == 0)
                return (0, 0, "No attributes to transfer.");

            int transferred = 0, errors = 0;
            string lastError = null;
            int total = toTransfer.Count;

            foreach (var (oldFP, newFP, _) in toTransfer)
            {
                try
                {
                    var coll = new ModelItemCollection();
                    coll.Add(newFP.Item);

                    // Prepare attributes
                    var attrs = oldFP.AutisAttributes ?? new List<AtributoCustom>();

                    // Prepare sets
                    Dictionary<ModelItem, List<SetAssignment>> setsMap = null;
                    if (config.TransferSets && oldFP.AutisSets != null && oldFP.AutisSets.Count > 0)
                    {
                        setsMap = new Dictionary<ModelItem, List<SetAssignment>>
                        {
                            [newFP.Item] = oldFP.AutisSets
                                .Select(s => new SetAssignment(s)).ToList()
                        };
                    }

                    if (config.TransferAttributes && attrs.Count > 0 ||
                        setsMap != null)
                    {
                        var result = AtributoService.GravarAtributos(
                            coll,
                            config.TransferAttributes ? attrs : null,
                            AutisSchema.CategoriaPrincipal,
                            setsMap);

                        if (result.erros == 0)
                            transferred++;
                        else
                        {
                            errors++;
                            lastError = result.mensagem;
                        }
                    }

                    int done = transferred + errors;
                    if (done % 20 == 0 || done == total)
                    {
                        int pct = (int)((double)done / total * 100);
                        progress?.Invoke($"Transferring: {done:N0}/{total:N0} elements...", pct);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    lastError = ex.Message;
                    Debug.WriteLine($"[Autis] Merge transfer error: {ex.Message}");
                }
            }

            var msg = $"Transferred: {transferred} element(s). Errors: {errors}.";
            if (errors > 0 && lastError != null)
                msg += $"\nLast error: {lastError}";
            return (transferred, errors, msg);
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────

        public static double LevenshteinRatio(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

            a = a.ToLowerInvariant();
            b = b.ToLowerInvariant();

            if (a == b) return 1.0;

            int lenA = a.Length, lenB = b.Length;
            var d = new int[lenA + 1, lenB + 1];

            for (int i = 0; i <= lenA; i++) d[i, 0] = i;
            for (int j = 0; j <= lenB; j++) d[0, j] = j;

            for (int i = 1; i <= lenA; i++)
            {
                for (int j = 1; j <= lenB; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(
                        d[i - 1, j] + 1,
                        d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return 1.0 - (double)d[lenA, lenB] / Math.Max(lenA, lenB);
        }

        public static double HierarchyOverlap(string pathA, string pathB)
        {
            if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB))
                return 0.0;

            var partsA = pathA.Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries);
            var partsB = pathB.Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries);

            if (partsA.Length == 0 || partsB.Length == 0) return 0.0;

            var setA = new HashSet<string>(partsA, StringComparer.OrdinalIgnoreCase);
            int overlap = partsB.Count(p => setA.Contains(p));

            return (double)overlap / Math.Max(partsA.Length, partsB.Length);
        }

        public static double BBoxCenterDistance(ElementFingerprint a, ElementFingerprint b)
        {
            if (!a.HasBBox || !b.HasBBox) return double.MaxValue;

            double dx = a.BBoxCenterX.Value - b.BBoxCenterX.Value;
            double dy = a.BBoxCenterY.Value - b.BBoxCenterY.Value;
            double dz = a.BBoxCenterZ.Value - b.BBoxCenterZ.Value;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static void SetIdField(ElementFingerprint fp, string field, string value)
        {
            switch (field)
            {
                case "IfcGUID":    if (fp.IfcGUID == null) fp.IfcGUID = value; break;
                case "ElementId":  if (fp.ElementId == null) fp.ElementId = value; break;
                case "UniqueId":   if (fp.UniqueId == null) fp.UniqueId = value; break;
                case "SourceGuid": if (fp.SourceGuid == null) fp.SourceGuid = value; break;
                case "ItemGuid":   if (fp.ItemGuid == null) fp.ItemGuid = value; break;
            }
        }

        private static void RemoveMatched(
            List<ElementFingerprint> oldFPs, List<ElementFingerprint> newFPs,
            List<(ElementFingerprint Old, ElementFingerprint New, MatchResult Result)> matches)
        {
            var oldSet = new HashSet<ElementFingerprint>(matches.Select(m => m.Old));
            var newSet = new HashSet<ElementFingerprint>(matches.Select(m => m.New));
            oldFPs.RemoveAll(fp => oldSet.Contains(fp));
            newFPs.RemoveAll(fp => newSet.Contains(fp));
        }

        private static void RemoveCandidates(
            List<ElementFingerprint> oldFPs, List<ElementFingerprint> newFPs,
            List<(ElementFingerprint Old, ElementFingerprint New, MatchResult Result)> candidates)
        {
            var oldSet = new HashSet<ElementFingerprint>(candidates.Select(c => c.Old));
            var newSet = new HashSet<ElementFingerprint>(candidates.Select(c => c.New));
            oldFPs.RemoveAll(fp => oldSet.Contains(fp));
            newFPs.RemoveAll(fp => newSet.Contains(fp));
        }

        private static Dictionary<string, List<ElementFingerprint>> BuildLookup(
            IEnumerable<ElementFingerprint> fingerprints,
            Func<ElementFingerprint, string> keyBuilder)
        {
            var lookup = new Dictionary<string, List<ElementFingerprint>>(StringComparer.OrdinalIgnoreCase);
            foreach (var fp in fingerprints)
            {
                var key = keyBuilder(fp);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (!lookup.TryGetValue(key, out var list))
                {
                    list = new List<ElementFingerprint>();
                    lookup[key] = list;
                }

                list.Add(fp);
            }

            return lookup;
        }

        private static IEnumerable<ElementFingerprint> FindCandidates(
            Dictionary<string, List<ElementFingerprint>> lookup,
            string key,
            HashSet<ElementFingerprint> matchedNew)
        {
            var normalized = NormalizeText(key) ?? "";
            if (!lookup.TryGetValue(normalized, out var items))
                return Enumerable.Empty<ElementFingerprint>();

            return items.Where(fp => !matchedNew.Contains(fp));
        }

        private static void AddCandidates(
            List<ElementFingerprint> target,
            HashSet<ElementFingerprint> seen,
            IEnumerable<ElementFingerprint> items)
        {
            foreach (var item in items)
            {
                if (seen.Add(item))
                    target.Add(item);
            }
        }

        private static double QuickSearchScore(ElementFingerprint oldFP, ElementFingerprint newFP)
        {
            double score = LevenshteinRatio(oldFP.DisplayName, newFP.DisplayName) * 2.0;

            if (!string.IsNullOrWhiteSpace(oldFP.TypeName) &&
                string.Equals(oldFP.TypeName, newFP.TypeName, StringComparison.OrdinalIgnoreCase))
                score += 2.0;

            if (!string.IsNullOrWhiteSpace(oldFP.CategoryName) &&
                string.Equals(oldFP.CategoryName, newFP.CategoryName, StringComparison.OrdinalIgnoreCase))
                score += 1.5;

            if (!string.IsNullOrWhiteSpace(oldFP.ClassName) &&
                string.Equals(oldFP.ClassName, newFP.ClassName, StringComparison.OrdinalIgnoreCase))
                score += 1.0;

            if (oldFP.HasBBox && newFP.HasBBox)
                score += Math.Max(0, 1.5 - (BBoxCenterDistance(oldFP, newFP) / 5.0));

            return score;
        }

        private static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var parts = value.Trim()
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Length == 0
                ? null
                : string.Join(" ", parts);
        }

        private static string FormatValue(VariantData value)
        {
            if (value == null) return "";
            switch (value.DataType)
            {
                case VariantDataType.Double:           return value.ToDouble().ToString("G");
                case VariantDataType.Int32:            return value.ToInt32().ToString();
                case VariantDataType.Boolean:          return value.ToBoolean() ? "True" : "False";
                case VariantDataType.DisplayString:    return value.ToDisplayString();
                case VariantDataType.IdentifierString: return value.ToIdentifierString();
                default:                               return value.ToString();
            }
        }

        private static string GetValueType(VariantData value)
        {
            if (value == null) return "string";
            switch (value.DataType)
            {
                case VariantDataType.Double:  return "double";
                case VariantDataType.Int32:   return "int";
                case VariantDataType.Boolean: return "bool";
                default:                      return "string";
            }
        }

        private static bool IsSetProperty(string propName)
        {
            if (string.IsNullOrWhiteSpace(propName)) return false;
            if (string.Equals(propName, AutisSchema.PropriedadeSets, StringComparison.OrdinalIgnoreCase))
                return true;
            return AutisSchema.PropriedadesSetsLegadas
                .Any(n => string.Equals(n, propName, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> SplitSets(string text)
        {
            return (text ?? "")
                .Split(new[] { '|', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;

namespace AutisAnalytics.NavisworksAtributos
{
    /// <summary>
    /// Modelo de um atributo customizado que será persistido no Navisworks.
    /// </summary>
    public class AtributoCustom
    {
        public string Categoria { get; set; }
        public string Nome      { get; set; }
        public string Valor     { get; set; }
        public string Tipo      { get; set; }   // "string" | "double" | "int" | "bool"

        public AtributoCustom(string categoria, string nome, string valor, string tipo = "string")
        {
            Categoria = categoria;
            Nome      = nome;
            Valor     = valor;
            Tipo      = tipo;
        }
    }

    /// <summary>
    /// Lê e grava atributos customizados via COM API do Navisworks 2026.
    ///
    /// ADAPTAÇÃO NW 2026:
    ///   - Namespace mudou para Autodesk.Navisworks.Api.Interop.ComApi
    ///   - InwOaPathArray removido → usar ToInwOaPath(ModelItem) por item
    ///   - SetCustomProperties removido → usar InwNodeAttributesColl via nós do path
    ///   - nwEUserDataType removido → valor é inferido pelo COM runtime
    ///   - InwOaCategoryVec/InwOaCategory removidos → usar InwOaPropertyAttribute
    ///   - ComApiBridge movido para Autodesk.Navisworks.Api.ComApi namespace
    /// </summary>
    public static class AtributoService
    {
        private const string CATEGORIA_PADRAO = AutisSchema.CategoriaPrincipal;

        // ─────────────────────────────────────────────────────────────────────
        // LEITURA — Managed API (sem alterações)
        // ─────────────────────────────────────────────────────────────────────

        public static List<AtributoCustom> LerPropriedades(ModelItem item)
        {
            var resultado = new List<AtributoCustom>();
            if (item == null) return resultado;

            foreach (var categoria in item.PropertyCategories)
                foreach (var prop in categoria.Properties)
                    resultado.Add(new AtributoCustom(
                        categoria.DisplayName ?? categoria.Name,
                        prop.DisplayName ?? prop.Name,
                        FormatarValor(prop.Value),
                        ObterTipo(prop.Value)));

            return resultado;
        }

        public static List<string> LerSetsSalvos(
            ModelItem item,
            IEnumerable<string> nomesSetsValidos = null,
            string nomeCategoria = null)
        {
            var resultado = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (item == null)
                return resultado.ToList();

            var nomesValidos = new HashSet<string>(
                (nomesSetsValidos ?? Enumerable.Empty<string>())
                    .Where(nome => !string.IsNullOrWhiteSpace(nome))
                    .Select(nome => nome.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var categoria in item.PropertyCategories)
            {
                var nomeCat = categoria.DisplayName ?? categoria.Name ?? "";
                if (!ObterCategoriasRelacionadas(nomeCategoria ?? CATEGORIA_PADRAO)
                    .Contains(nomeCat, StringComparer.OrdinalIgnoreCase))
                    continue;

                foreach (var prop in categoria.Properties)
                {
                    var nomeProp = (prop.DisplayName ?? prop.Name ?? "").Trim();
                    var valorProp = FormatarValor(prop.Value)?.Trim() ?? "";

                    if (EhPropriedadeDeSets(nomeProp))
                    {
                        foreach (var nomeSet in SepararListaSets(valorProp))
                        {
                            if (nomesValidos.Count == 0 || nomesValidos.Contains(nomeSet))
                                resultado.Add(nomeSet);
                        }
                        continue;
                    }

                    // Suporte para formato legado: um set por propriedade.
                    if (nomesValidos.Count > 0 &&
                        nomesValidos.Contains(nomeProp) &&
                        string.Equals(valorProp, nomeProp, StringComparison.OrdinalIgnoreCase))
                    {
                        resultado.Add(nomeProp);
                    }
                }
            }

            return resultado
                .OrderBy(nome => nome, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // ESCRITA — COM API adaptada para NW 2026
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Grava atributos nos elementos selecionados usando a mesma abordagem do eT.ools:
        ///   state.BeginEdit → guiNode.SetUserDefined() → state.EndEdit
        /// </summary>
        public static (int sucesso, int erros, string mensagem) GravarAtributos(
            ModelItemCollection itens,
            List<AtributoCustom> atributos,
            string nomeCategoria = null,
            Dictionary<ModelItem, List<SetAssignment>> setsPorItem = null)
        {
            if (itens  == null || itens.Count  == 0) return (0, 0, "No elements selected.");

            bool temAtributos = atributos != null && atributos.Count > 0;
            bool temSets = setsPorItem != null && setsPorItem.Values.Any(sets => sets != null && sets.Count > 0);
            if (!temAtributos && !temSets) return (0, 0, "No attributes or sets to write.");
            atributos = atributos ?? new List<AtributoCustom>();

            var categoria = string.IsNullOrWhiteSpace(nomeCategoria)
                ? CATEGORIA_PADRAO
                : nomeCategoria.Trim();
            var internalName = categoria.Replace(" ", "_") + "_Internal";
            ComApi.InwOpState10 oState = null;
            bool editStarted = false;

            try
            {
                oState = (ComApi.InwOpState10)ComBridgeHelper.ObterEstado();
                oState.BeginEdit("autis_gravar");
                editStarted = true;

                int sucesso = 0, erros = 0;
                string ultimoErro = null;

                foreach (ModelItem item in itens)
                {
                    try
                    {
                        // Converter para seleção/path (mesmo padrão do eT.ools)
                        var itemColl = new ModelItemCollection();
                        itemColl.Add(item);
                        var sel = ComApiBridge.ToInwOpSelection(itemColl);
                        var paths = sel.Paths();
                        var path = (ComApi.InwOaPath3)(dynamic)paths.Last();

                        // Obter GUI property node
                        var guiNode = (ComApi.InwGUIPropertyNode2)oState.GetGUIPropertyNode(path, true);

                        // Remover a categoria atual e categorias legadas antes de recriar.
                        RemoverCategoriasRelacionadas(guiNode, categoria);

                        // Criar InwOaPropertyVec com as propriedades
                        var propVec = (ComApi.InwOaPropertyVec)(dynamic)oState.ObjectFactory(
                            ComApi.nwEObjectType.eObjectType_nwOaPropertyVec);

                        var nomesInternosUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        int propriedadesAdicionadas = 0;
                        foreach (var atr in atributos)
                        {
                            if (string.IsNullOrWhiteSpace(atr?.Nome)) continue;

                            AdicionarPropriedade(oState, propVec, atr.Nome, ConverterValor(atr), nomesInternosUsados);
                            propriedadesAdicionadas++;
                        }

                        if (setsPorItem != null &&
                            setsPorItem.TryGetValue(item, out var setsDoItem) &&
                            setsDoItem != null)
                        {
                            var nomesSets = setsDoItem
                                .Where(setInfo => !string.IsNullOrWhiteSpace(setInfo?.Nome))
                                .Select(setInfo => setInfo.Nome.Trim())
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(nome => nome, StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            if (nomesSets.Count > 0)
                            {
                                AdicionarPropriedade(
                                    oState,
                                    propVec,
                                    AutisSchema.PropriedadeSets,
                                    string.Join(" | ", nomesSets),
                                    nomesInternosUsados);
                                propriedadesAdicionadas++;
                            }
                        }

                        if (propriedadesAdicionadas == 0)
                            continue;

                        // Gravar! (mesma chamada do eT.ools)
                        guiNode.SetUserDefined(0, categoria, internalName, propVec);
                        sucesso++;
                    }
                    catch (Exception ex)
                    {
                        erros++;
                        ultimoErro = ex.Message;
                        System.Diagnostics.Debug.WriteLine($"[Autis] Erro no item: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                var msg = $"Saved: {sucesso} element(s). Errors: {erros}.";
                if (erros > 0 && ultimoErro != null)
                    msg += $"\n\nLast error: {ultimoErro}";
                return (sucesso, erros, msg);
            }
            catch (Exception ex)
            {
                return (0, itens.Count, $"Error accessing COM API: {ex.Message}");
            }
            finally
            {
                if (editStarted)
                {
                    try { oState.EndEdit(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Autis] Erro ao finalizar edicao COM: {ex.Message}");
                    }
                }
            }
        }

        public static (int sucesso, int erros, string mensagem) ExcluirAtributos(
            ModelItemCollection itens,
            string nomeCategoria = null)
        {
            if (itens == null || itens.Count == 0) return (0, 0, "No elements selected.");

            var categoria = string.IsNullOrWhiteSpace(nomeCategoria)
                ? CATEGORIA_PADRAO
                : nomeCategoria.Trim();
            ComApi.InwOpState10 oState = null;
            bool editStarted = false;

            try
            {
                oState = (ComApi.InwOpState10)ComBridgeHelper.ObterEstado();
                oState.BeginEdit("autis_excluir");
                editStarted = true;

                int removidos = 0, naoEncontrados = 0, erros = 0;
                string ultimoErro = null;

                foreach (ModelItem item in itens)
                {
                    try
                    {
                        var itemColl = new ModelItemCollection();
                        itemColl.Add(item);
                        var sel = ComApiBridge.ToInwOpSelection(itemColl);
                        var paths = sel.Paths();
                        var path = (ComApi.InwOaPath3)(dynamic)paths.Last();

                        var guiNode = (ComApi.InwGUIPropertyNode2)oState.GetGUIPropertyNode(path, true);

                        if (RemoverCategoriasRelacionadas(guiNode, categoria))
                            removidos++;
                        else
                            naoEncontrados++;
                    }
                    catch (Exception ex)
                    {
                        erros++;
                        ultimoErro = ex.Message;
                        System.Diagnostics.Debug.WriteLine($"[Autis] Erro ao excluir no item: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                var msg = $"Removed: {removidos} element(s). Not found: {naoEncontrados}. Errors: {erros}.";
                if (erros > 0 && ultimoErro != null)
                    msg += $"\n\nLast error: {ultimoErro}";
                return (removidos, erros, msg);
            }
            catch (Exception ex)
            {
                return (0, itens.Count, $"Error accessing COM API: {ex.Message}");
            }
            finally
            {
                if (editStarted)
                {
                    try { oState.EndEdit(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Autis] Erro ao finalizar exclusao COM: {ex.Message}");
                    }
                }
            }
        }

        private static void AdicionarPropriedade(ComApi.InwOpState10 oState,
            ComApi.InwOaPropertyVec propVec, string nome, object valor,
            HashSet<string> nomesInternosUsados)
        {
            var prop = (ComApi.InwOaProperty)(dynamic)oState.ObjectFactory(
                ComApi.nwEObjectType.eObjectType_nwOaProperty);

            prop.name = CriarNomeInternoPropriedade(nome, nomesInternosUsados);
            prop.UserName = nome;
            prop.value = valor ?? "";

            propVec.Properties().Add(prop);
        }

        private static string CriarNomeInternoPropriedade(string nome,
            HashSet<string> nomesInternosUsados)
        {
            if (nomesInternosUsados == null)
                nomesInternosUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(nome))
                nome = "prop";

            var baseName = new string(nome
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray())
                .Trim('_');

            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "prop";

            string candidate = baseName + "_prop";
            int suffix = 2;
            while (!nomesInternosUsados.Add(candidate))
                candidate = $"{baseName}_{suffix++}_prop";

            return candidate;
        }

        private static bool RemoverCategoriasRelacionadas(
            ComApi.InwGUIPropertyNode2 guiNode,
            string nomeCategoriaPrincipal)
        {
            bool removeuAlguma = false;

            foreach (var nomeCategoria in ObterCategoriasRelacionadas(nomeCategoriaPrincipal))
            {
                if (RemoverCategoriaExistente(guiNode, nomeCategoria))
                    removeuAlguma = true;
            }

            return removeuAlguma;
        }

        private static IEnumerable<string> ObterCategoriasRelacionadas(string nomeCategoriaPrincipal)
        {
            var nomes = new List<string>();

            if (!string.IsNullOrWhiteSpace(nomeCategoriaPrincipal))
                nomes.Add(nomeCategoriaPrincipal.Trim());

            nomes.AddRange(AutisSchema.CategoriasLegadas);

            return nomes
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Remove categoria user-defined existente pelo DisplayName (evita duplicação).
        /// Mesmo padrão do eT.ools DeleteEToolsCategory.
        /// </summary>
        private static bool RemoverCategoriaExistente(ComApi.InwGUIPropertyNode2 guiNode, string nomeCategoria)
        {
            int idx = 1;
            foreach (ComApi.InwGUIAttribute2 attr in guiNode.GUIAttributes())
            {
                try
                {
                    if (attr.UserDefined && attr.ClassUserName == nomeCategoria)
                    {
                        guiNode.RemoveUserDefined(idx);
                        return true; // Removeu — sai (só pode ter 1 com esse nome)
                    }
                    if (attr.UserDefined) idx++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Autis] Erro ao remover categoria '{nomeCategoria}': {ex.Message}");
                }
            }

            return false;
        }

        /// <summary>
        /// Converte o valor do atributo para o tipo COM adequado.
        /// </summary>
        private static object ConverterValor(AtributoCustom atr)
        {
            switch (atr.Tipo?.ToLower())
            {
                case "double":
                case "float":
                    if (double.TryParse(atr.Valor?.Replace(",", "."),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double dv))
                        return dv;
                    return atr.Valor ?? "";

                case "int":
                    if (int.TryParse(atr.Valor, out int iv))
                        return iv;
                    return atr.Valor ?? "";

                case "bool":
                    bool bv;
                    if (!bool.TryParse(atr.Valor, out bv))
                        bv = atr.Valor?.ToLower() == "yes" || atr.Valor?.ToLower() == "true";
                    return bv;

                default:
                    return atr.Valor ?? "";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers de formatação (leitura)
        // ─────────────────────────────────────────────────────────────────────

        private static string FormatarValor(VariantData valor)
        {
            if (valor == null) return "";
            switch (valor.DataType)
            {
                case VariantDataType.Double:           return valor.ToDouble().ToString("G");
                case VariantDataType.Int32:            return valor.ToInt32().ToString();
                case VariantDataType.Boolean:          return valor.ToBoolean() ? "True" : "False";
                case VariantDataType.DisplayString:    return valor.ToDisplayString();
                case VariantDataType.IdentifierString: return valor.ToIdentifierString();
                default:                               return valor.ToString();
            }
        }

        private static string ObterTipo(VariantData valor)
        {
            if (valor == null) return "string";
            switch (valor.DataType)
            {
                case VariantDataType.Double:  return "double";
                case VariantDataType.Int32:   return "int";
                case VariantDataType.Boolean: return "bool";
                default:                      return "string";
            }
        }

        private static IEnumerable<string> SepararListaSets(string texto)
        {
            return (texto ?? "")
                .Split(new[] { '|', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(parte => parte.Trim())
                .Where(parte => !string.IsNullOrWhiteSpace(parte));
        }

        private static bool EhPropriedadeDeSets(string nomePropriedade)
        {
            if (string.IsNullOrWhiteSpace(nomePropriedade))
                return false;

            if (string.Equals(nomePropriedade, AutisSchema.PropriedadeSets, StringComparison.OrdinalIgnoreCase))
                return true;

            return AutisSchema.PropriedadesSetsLegadas
                .Any(nome => string.Equals(nome, nomePropriedade, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Descobre e invoca o ComApiBridge em runtime, sem depender do namespace
    /// exato da DLL — que mudou do NW 2025 para o NW 2026.
    ///
    /// Ordem de tentativa:
    ///   1. Autodesk.Navisworks.Api.ComApi.ComApiBridge   (NW 2026 — namespace real)
    ///   2. Autodesk.Navisworks.ComApi.ComApiBridge       (NW 2026 — alternativo)
    ///
    /// Se nenhum for encontrado, lança InvalidOperationException descritiva.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    internal static class ComBridgeHelper
    {
        // Candidatos de namespace, por ordem de prioridade (NW 2026 primeiro)
        private static readonly string[] BRIDGE_CANDIDATES = new[]
        {
            "Autodesk.Navisworks.Api.ComApi.ComApiBridge",  // NW 2026 (namespace real)
            "Autodesk.Navisworks.ComApi.ComApiBridge",      // NW 2026 (alternativo)
        };

        private static Type   _bridgeType;   // cacheado após primeira descoberta
        private static object _lockObj = new object();

        // ── Descoberta do tipo ────────────────────────────────────────────────
        private static Type ObterTipoBridge()
        {
            if (_bridgeType != null) return _bridgeType;

            lock (_lockObj)
            {
                if (_bridgeType != null) return _bridgeType;

                // Varre todos os assemblies já carregados no AppDomain
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var candidato in BRIDGE_CANDIDATES)
                    {
                        try
                        {
                            var tipo = asm.GetType(candidato);
                            if (tipo != null)
                            {
                                _bridgeType = tipo;
                                System.Diagnostics.Debug.WriteLine(
                                    $"[Autis] ComApiBridge encontrado: {candidato} em {asm.GetName().Name}");
                                return _bridgeType;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[Autis] Falha ao inspecionar assembly '{asm.GetName().Name}' para '{candidato}': {ex.Message}");
                        }
                    }
                }

                // Não achou nos assemblies carregados — tenta forçar o carregamento
                _bridgeType = TentarCarregarDllComApi();

                if (_bridgeType == null)
                    throw new InvalidOperationException(
                        "ComApiBridge not found in any loaded assembly.\n\n" +
                        "Check:\n" +
                        "  1. Autodesk.Navisworks.ComApi.dll is referenced in the .csproj\n" +
                        "  2. The NavisworksDir path points to the correct NW 2026 installation\n" +
                        $"  Candidates tried:\n     - {string.Join("\n     - ", BRIDGE_CANDIDATES)}");

                return _bridgeType;
            }
        }

        private static Type TentarCarregarDllComApi()
        {
            var dllsNome = new[] { "Autodesk.Navisworks.ComApi", "Autodesk.Navisworks.Api.ComApi" };

            foreach (var nome in dllsNome)
            {
                try
                {
                    var asm = Assembly.Load(nome);
                    foreach (var candidato in BRIDGE_CANDIDATES)
                    {
                        var tipo = asm?.GetType(candidato);
                        if (tipo != null) return tipo;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Autis] Falha ao carregar assembly '{nome}': {ex.Message}");
                }
            }

            return null;
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>
        /// Equivalente a: ComApiBridge.State
        /// Retorna o InwOpState do documento ativo.
        /// </summary>
        public static ComApi.InwOpState ObterEstado()
        {
            var bridge = ObterTipoBridge();
            var prop   = bridge.GetProperty("State",
                BindingFlags.Public | BindingFlags.Static);

            if (prop == null)
                throw new InvalidOperationException(
                    "Property 'State' not found in ComApiBridge.");

            var state = prop.GetValue(null) as ComApi.InwOpState;
            if (state == null)
                throw new InvalidOperationException(
                    "ComApiBridge.State retornou null — nenhum documento ativo?");

            return state;
        }

        /// <summary>
        /// NW 2026: ComApiBridge.ToInwOaPath(ModelItem) — converte um ModelItem para InwOaPath.
        /// (ToInwOaPathArray foi removido; agora é um item por vez)
        /// </summary>
        public static ComApi.InwOaPath ObterPath(ModelItem item)
        {
            var bridge = ObterTipoBridge();

            MethodInfo metodo = bridge.GetMethod("ToInwOaPath",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(ModelItem) },
                null);

            if (metodo == null)
                throw new InvalidOperationException(
                    "Method 'ToInwOaPath(ModelItem)' not found in ComApiBridge.\n" +
                    "Check that Autodesk.Navisworks.ComApi.dll is compatible with NW 2026.");

            var path = metodo.Invoke(null, new object[] { item }) as ComApi.InwOaPath;
            if (path == null)
                throw new InvalidOperationException(
                    "ToInwOaPath retornou null para o item fornecido.");

            return path;
        }
    }
}

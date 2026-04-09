# MERGE NWD — Especificacao Completa

## O Problema

Quando um modelo NWD e revisado (nova versao do arquivo), o Navisworks **perde**:
1. **Atributos customizados** (`Autis_Attributes`) gravados nos elementos
2. **Selection Sets** que agrupavam elementos
3. **Qualquer dado** associado ao modelo antigo

O usuario precisa recarregar o modelo novo e **transferir** tudo que existia no antigo.

---

## Visao Geral da Solucao

Novo botao **"Merge NWD"** no ribbon que:

1. Usuario seleciona o **novo NWD** (FileOpenDialog)
2. Plugin **carrega o NWD antigo** (modelo atual no documento) e o **NWD novo** como referencia temporaria
3. **Extrai fingerprint** de todos os elementos de ambos os modelos
4. **Compara** usando 5 niveis de profundidade (cascata)
5. **Apresenta relatorio** visual: matched, added, removed, conflitos
6. Usuario **confirma** o merge
7. Plugin **executa**: transfere atributos, recria sets, remove modelo antigo, mantem novo

---

## Arquitetura de Matching — 5 Niveis em Cascata

O merge tenta o nivel mais confiavel primeiro. Elementos nao pareados descem para o proximo nivel.

```
┌─────────────────────────────────────────────────────────────┐
│  NIVEL 1 — EXATO (ID Unico)                                │
│  Confianca: 100%   |   Automatico                           │
│                                                             │
│  Busca propriedades de identificacao unica:                 │
│    • IfcGUID (IFC models)                                   │
│    • Element ID (Revit/BIM)                                 │
│    • UniqueId / GUID                                        │
│    • LcOaNode:SourceGuid                                    │
│    • Item > GUID                                            │
│                                                             │
│  Match: valor identico entre old e new → pareado            │
│  Complexidade: O(n) com HashMap lookup                      │
└──────────────────────────┬──────────────────────────────────┘
                           │ nao pareados ↓
┌──────────────────────────▼──────────────────────────────────┐
│  NIVEL 2 — BASICO (ID + Nome)                               │
│  Confianca: 90-95%   |   Automatico                         │
│                                                             │
│  Para elementos sem ID unico ou com ID alterado:            │
│    • Match por: ElementID + DisplayName                     │
│    • Ou: TypeName + DisplayName                             │
│    • Ou: Category + Name (fallback)                         │
│                                                             │
│  Chave composta: concat das 2 propriedades                  │
│  Match exato da chave composta → pareado                    │
│  Complexidade: O(n) com HashMap                             │
└──────────────────────────┬──────────────────────────────────┘
                           │ nao pareados ↓
┌──────────────────────────▼──────────────────────────────────┐
│  NIVEL 3 — MEDIO (ID + Nome + Geometria)                    │
│  Confianca: 75-90%   |   Automatico com threshold           │
│                                                             │
│  Scoring ponderado (0-100):                                 │
│                                                             │
│    Componente            Peso    Criterio                   │
│    ─────────────────     ────    ─────────────────────────  │
│    Nome similar           30%    Levenshtein ratio >= 0.8   │
│    Tipo/Categoria igual   25%    ClassDisplayName match     │
│    BoundingBox proximo    25%    Centro a centro < 0.5m     │
│    Hierarquia similar     20%    Path ancestry overlap      │
│                                                             │
│  Score >= 80 → pareado automatico                           │
│  Score 60-79 → candidato (revisao manual)                   │
│  Score < 60 → descartado, segue para nivel 4               │
│  Complexidade: O(n*m) limitado por tipo/categoria           │
└──────────────────────────┬──────────────────────────────────┘
                           │ nao pareados ↓
┌──────────────────────────▼──────────────────────────────────┐
│  NIVEL 4 — PROFUNDO (Fingerprint Completo)                  │
│  Confianca: 60-80%   |   Revisao manual recomendada         │
│                                                             │
│  Para cada elemento nao pareado:                            │
│    1. Serializar TODAS as propriedades como key=value pairs │
│    2. Gerar "fingerprint" = Set<string> de propriedades     │
│    3. Calcular Jaccard Similarity entre old e new:          │
│       J(A,B) = |A ∩ B| / |A ∪ B|                           │
│    4. Combinar com:                                         │
│       - Distancia geometrica normalizada (0-1)              │
│       - Overlap de propriedades numericas (tolerancia 5%)   │
│       - Match de propriedades raras (alta entropia)         │
│                                                             │
│  Peso final:                                                │
│    40% Jaccard similarity                                   │
│    25% Geometric proximity                                  │
│    20% Numeric property match                               │
│    15% Rare property match                                  │
│                                                             │
│  Score >= 70 → candidato (revisao manual)                   │
│  Score < 70 → descartado, segue para nivel 5               │
│  Complexidade: O(n*m) com poda por tipo                     │
└──────────────────────────┬──────────────────────────────────┘
                           │ nao pareados ↓
┌──────────────────────────▼──────────────────────────────────┐
│  NIVEL 5 — ULTRA PROFUNDO (IA Semantica)                    │
│  Confianca: variavel   |   Sempre revisao manual            │
│                                                             │
│  Para os elementos "orfaos" restantes:                      │
│                                                             │
│  Estrategia A — Embedding Local (sem API externa):          │
│    1. Serializar propriedades em texto natural:             │
│       "Wall | Concrete Wall 300mm | Level 2 | h=3.5m"      │
│    2. Gerar vetor TF-IDF de cada texto                     │
│    3. Cosine similarity entre vetores old vs new            │
│    4. Top-3 candidatos com score > 0.6                     │
│                                                             │
│  Estrategia B — LLM API (opcional, Claude):                 │
│    1. Agrupar orfaos por tipo/categoria                     │
│    2. Enviar batch para Claude API:                         │
│       "Dado o elemento antigo com propriedades [X]          │
│        e estes candidatos novos [Y1, Y2, Y3],              │
│        qual e o melhor match? Retorne JSON."                │
│    3. Claude retorna match + confianca + justificativa      │
│                                                             │
│  Estrategia C — Hibrida:                                    │
│    1. TF-IDF para pre-filtrar top-10 candidatos             │
│    2. LLM para decidir entre os top-10                      │
│    3. Melhor custo-beneficio                                │
│                                                             │
│  Resultado: sempre apresentado para aprovacao manual         │
│  Complexidade: O(n*m) ou O(n*k) com pre-filtro             │
└─────────────────────────────────────────────────────────────┘
```

---

## Fluxo do Usuario (UX)

```
[1] Usuario clica "Merge NWD" no ribbon
         │
[2] FileOpenDialog: "Selecione o NOVO NWD revisado"
         │
[3] Tela de configuracao:
    ┌─────────────────────────────────────────────────┐
    │  MERGE NWD — Configuracao                       │
    │                                                 │
    │  Modelo Atual: project_v1.nwd                   │
    │  Modelo Novo:  project_v2.nwd                   │
    │                                                 │
    │  O que transferir:                              │
    │  [x] Atributos Customizados (Autis_Attributes)  │
    │  [x] Associacoes de Sets (Autis_AWP)            │
    │  [ ] Outros atributos user-defined              │
    │                                                 │
    │  Profundidade de matching:                      │
    │  (●) Automatico (Niveis 1-3, rapido)            │
    │  ( ) Profundo (Niveis 1-4, mais lento)          │
    │  ( ) Ultra (Niveis 1-5 com IA, completo)        │
    │                                                 │
    │  Propriedade de ID preferida:                   │
    │  [  IfcGUID           ▼]  (auto-detectado)      │
    │                                                 │
    │  [  Analisar Modelos  ]                         │
    └─────────────────────────────────────────────────┘
         │
[4] Processamento com ProgressBar:
    "Extraindo fingerprints... (1/4)"
    "Nivel 1: Matching por ID unico... (2/4)"
    "Nivel 2: Matching basico... (3/4)"
    "Nivel 3: Matching geometrico... (4/4)"
         │
[5] Tela de Resultados (o coracao do merge):
    ┌─────────────────────────────────────────────────────────────┐
    │  RESULTADO DO MERGE                                        │
    │                                                            │
    │  ┌──────────────────────────────────────────────────────┐  │
    │  │  Resumo                                              │  │
    │  │  ■ 1.247 Pareados (Nivel 1: 980, N2: 200, N3: 67)   │  │
    │  │  ■ 45 Novos (sem correspondencia no antigo)          │  │
    │  │  ■ 23 Removidos (existiam no antigo, nao no novo)    │  │
    │  │  ■ 12 Candidatos (match incerto, revisao necessaria) │  │
    │  │  ■ 892 Atributos a transferir                        │  │
    │  │  ■ 15 Sets a reconstruir                             │  │
    │  └──────────────────────────────────────────────────────┘  │
    │                                                            │
    │  [Tab: Pareados] [Tab: Novos] [Tab: Removidos]             │
    │  [Tab: Candidatos] [Tab: Conflitos]                        │
    │                                                            │
    │  ┌──────────────────────────────────────────────────────┐  │
    │  │ Tab Candidatos (revisao manual):                     │  │
    │  │                                                      │  │
    │  │ Old Element          New Element       Score  Acao   │  │
    │  │ ──────────────────   ──────────────    ─────  ────── │  │
    │  │ Wall-Concrete-042    Wall-Conc-042B    78%    [✓][✗] │  │
    │  │ Door-Type1-003       Door-TypeA-003    72%    [✓][✗] │  │
    │  │ Pipe-DN100-seg12     Pipe-DN100-s12    65%    [✓][✗] │  │
    │  │                                                      │  │
    │  │ Click [✓] para aceitar match, [✗] para rejeitar      │  │
    │  └──────────────────────────────────────────────────────┘  │
    │                                                            │
    │  [Exportar Relatorio CSV]  [Executar Merge]  [Cancelar]    │
    └─────────────────────────────────────────────────────────────┘
         │
[6] Confirmacao final:
    "Transferir 892 atributos e reconstruir 15 sets?
     Esta acao nao pode ser desfeita."
    [Confirmar] [Cancelar]
         │
[7] Execucao com progresso:
    "Transferindo atributos... (450/892)"
    "Reconstruindo sets... (8/15)"
         │
[8] Relatorio final:
    "Merge concluido!
     ✓ 890 atributos transferidos (2 erros)
     ✓ 15 sets reconstruidos
     ✓ 45 novos elementos sem atributos
     ✓ 23 elementos removidos (atributos perdidos)"
```

---

## Estrutura de Dados

### ElementFingerprint (extraido de cada ModelItem)

```csharp
internal class ElementFingerprint
{
    // Identificacao
    public ModelItem Item { get; set; }
    public string ModelSource { get; set; }       // nome do arquivo NWD de origem

    // IDs unicos (Nivel 1)
    public string IfcGUID { get; set; }           // Element > IfcGUID
    public string ElementId { get; set; }         // Element > Element ID
    public string UniqueId { get; set; }          // Element > UniqueId
    public string SourceGuid { get; set; }        // LcOaNode > SourceGuid
    public string ItemGuid { get; set; }          // Item > GUID

    // Identificacao basica (Nivel 2)
    public string DisplayName { get; set; }       // ModelItem.DisplayName
    public string ClassName { get; set; }         // ModelItem.ClassDisplayName
    public string TypeName { get; set; }          // Element > Type ou Type > Name
    public string CategoryName { get; set; }      // Element > Category

    // Geometria (Nivel 3)
    public Point3D BBoxCenter { get; set; }       // Centro do BoundingBox
    public Vector3D BBoxSize { get; set; }        // Dimensoes do BoundingBox
    public string HierarchyPath { get; set; }     // "Model > Level 2 > Walls > Wall-001"

    // Propriedades completas (Nivel 4-5)
    public Dictionary<string, string> AllProperties { get; set; }
    public HashSet<string> PropertyFingerprint { get; set; } // "Cat|Prop|Value" set

    // Atributos Autis existentes (o que queremos transferir)
    public List<AtributoCustom> AutisAttributes { get; set; }
    public List<string> AutisSets { get; set; }   // nomes dos sets salvos

    // Resultado do matching
    public MatchResult Match { get; set; }
}
```

### MatchResult

```csharp
internal class MatchResult
{
    public MatchStatus Status { get; set; }       // Matched, New, Removed, Candidate
    public int Level { get; set; }                // 1-5, nivel que encontrou o match
    public double Score { get; set; }             // 0-100, confianca
    public ElementFingerprint Partner { get; set; } // elemento pareado (old↔new)
    public string Justification { get; set; }     // descricao do porque pareou
}

internal enum MatchStatus
{
    Matched,    // pareado com confianca >= threshold
    Candidate,  // pareado mas abaixo do threshold, precisa revisao
    New,        // existe no novo, nao no antigo
    Removed,    // existe no antigo, nao no novo
    Conflict    // multiplos matches com score similar
}
```

### MergeConfig

```csharp
internal class MergeConfig
{
    public string NewNwdPath { get; set; }
    public bool TransferAttributes { get; set; } = true;
    public bool TransferSets { get; set; } = true;
    public bool TransferOtherUserDefined { get; set; } = false;
    public MergeDepth Depth { get; set; } = MergeDepth.Automatic;
    public string PreferredIdProperty { get; set; } // auto-detected
    public double AutoMatchThreshold { get; set; } = 80.0;
    public double CandidateThreshold { get; set; } = 60.0;
}

internal enum MergeDepth
{
    Automatic,  // Niveis 1-3
    Deep,       // Niveis 1-4
    Ultra       // Niveis 1-5
}
```

---

## Logica de Extracao de Fingerprint

```
Para cada ModelItem do modelo:
  1. Iterar item.PropertyCategories
  2. Buscar propriedades de ID em categorias conhecidas:
     - "Element" > "IfcGUID", "Element ID", "UniqueId", "GUID"
     - "LcOaNode" > "SourceGuid", "Guid"
     - "Item" > "GUID", "Id"
     - "Revit" > "ElementId"
  3. Capturar DisplayName, ClassDisplayName
  4. Capturar BoundingBox se disponivel (item.BoundingBox)
  5. Construir HierarchyPath percorrendo item.Ancestors
  6. Para Nivel 4+: capturar TODAS propriedades como Dict
  7. Capturar atributos Autis existentes (usando AtributoService.LerPropriedades
     filtrado por AutisSchema.CategoriaPrincipal)
  8. Capturar sets salvos (usando AtributoService.LerSetsSalvos)
```

### Auto-deteccao do campo ID preferido

```
1. Amostrar 50 elementos aleatorios do modelo antigo
2. Para cada campo ID candidato, contar quantos tem valor nao-nulo
3. Verificar unicidade (nao tem duplicados)
4. Selecionar o campo com maior cobertura + unicidade
5. Mostrar para o usuario com opcao de override
```

---

## Logica de Matching Detalhada

### Nivel 1 — Match Exato por ID

```
1. Construir HashMap<string, ElementFingerprint> para modelo NOVO:
   - Chave: valor do ID preferido (ex: IfcGUID)
   - Se elemento tem multiplos IDs, registrar todos

2. Para cada elemento do modelo ANTIGO que tem Autis attributes:
   - Buscar ID preferido no HashMap
   - Se encontrou → MATCH (Level=1, Score=100)
   - Marcar ambos como "pareados"

3. Se campo preferido nao matchou, tentar campos alternativos:
   IfcGUID → ElementId → UniqueId → SourceGuid → ItemGuid
```

### Nivel 2 — Match por Chave Composta

```
1. Para nao-pareados do Nivel 1:
   Construir chaves compostas no modelo NOVO:
   - Key1: ElementID + "|" + DisplayName
   - Key2: TypeName + "|" + DisplayName
   - Key3: ClassName + "|" + DisplayName

2. Buscar cada chave composta do ANTIGO nos HashMaps
3. Se encontrou → MATCH (Level=2, Score=92)
```

### Nivel 3 — Scoring Ponderado

```
1. Agrupar nao-pareados por ClassName (reduz espaco de busca)
2. Para cada par (old, new) no mesmo grupo:
   a. name_score = LevenshteinRatio(old.DisplayName, new.DisplayName) * 30
   b. type_score = (old.TypeName == new.TypeName) ? 25 : 0
   c. geo_score  = max(0, 25 * (1 - Distance(old.Center, new.Center) / 2.0))
   d. hier_score = HierarchyOverlap(old.Path, new.Path) * 20
   e. total = a + b + c + d

3. Para cada old, pegar o new com maior score:
   - Se score >= 80 → MATCH (Level=3)
   - Se score 60-79 → CANDIDATE (Level=3)
   - Se dois news tem score similar (diff < 5) → CONFLICT
```

### Nivel 4 — Fingerprint Jaccard

```
1. Gerar PropertyFingerprint para cada nao-pareado:
   Set de strings: "Category|PropertyName|Value"
   Excluir propriedades voláteis: timestamps, internal IDs

2. Para cada par (old, new):
   a. jaccard = |old ∩ new| / |old ∪ new|
   b. geo_proximity = normalizar distancia 0-1
   c. numeric_match = comparar props numericas com 5% tolerancia
   d. rare_match = props que existem em < 10% dos elementos

   score = jaccard*40 + geo*25 + numeric*20 + rare*15

3. Score >= 70 → CANDIDATE
```

### Nivel 5 — IA Semantica

```
Estrategia Hibrida (recomendada):

1. Para cada orfao ANTIGO com atributos:
   a. Serializar: "Type: Concrete Wall | Name: W-042 |
      Level: 2 | Height: 3.5m | Width: 300mm | Area: 12.5m2"

2. Para candidatos NOVOS do mesmo tipo/categoria:
   a. Serializar igualmente
   b. Calcular TF-IDF vectors
   c. Cosine similarity → top-5 candidatos

3. Se usuario habilitou LLM (Claude API):
   Prompt para Claude:
   ────────────────────────────────────────────────
   You are matching construction elements between two revisions of a BIM model.

   OLD ELEMENT (has attributes to transfer):
   {serialized_old_element}

   CANDIDATE NEW ELEMENTS:
   1. {serialized_candidate_1}
   2. {serialized_candidate_2}
   3. {serialized_candidate_3}

   Return JSON:
   {
     "best_match": 1|2|3|null,
     "confidence": 0.0-1.0,
     "reason": "brief explanation"
   }

   Rules:
   - Return null if no good match exists
   - confidence < 0.5 means uncertain
   - Consider: same type, similar dimensions, similar location, similar name
   ────────────────────────────────────────────────

4. Resultado sempre vai para revisao manual
```

---

## Execucao do Merge

Apos usuario confirmar:

```
1. BACKUP: Salvar snapshot dos atributos atuais em CSV (seguranca)

2. Para cada par MATCHED/ACCEPTED:
   a. Ler atributos Autis do old element
   b. Gravar no new element via AtributoService.GravarAtributos()

3. Para SETS:
   a. Coletar mapeamento: old_set_items → new_set_items
   b. Para cada set afetado:
      - Criar nova lista de items (matched new elements)
      - Atualizar propriedade Autis_AWP nos new elements

4. Para outros user-defined (se habilitado):
   a. Ler categorias user-defined do old element
   b. Recriar no new element

5. LIMPEZA:
   a. Opcionalmente remover modelo antigo do documento
   b. Ou manter ambos para comparacao
```

---

## Arquivos a Criar/Modificar

```
NOVOS:
  MergeService.cs              — Logica de fingerprint, matching, scoring
  MergeForm.cs                 — UI completa (config + resultados + revisao)
  MergeModels.cs               — DTOs: ElementFingerprint, MatchResult, MergeConfig, MergeReport
  TextSimilarity.cs            — Levenshtein, TF-IDF, Jaccard (utilitarios)

MODIFICAR:
  AutisAtributosPlugin.cs      — Adicionar 5o botao "Merge NWD" no ribbon
  AutisSchema.cs               — (se necessario) constantes de merge
  AtributoService.cs           — Expor metodo para ler TODAS categorias user-defined
```

---

## Consideracoes Tecnicas

### Performance
- Modelos grandes: 50k-500k elementos
- Nivel 1-2: O(n) com HashMaps — rapido (< 5s para 100k elementos)
- Nivel 3: O(n*m) mas agrupado por tipo — aceitavel (< 30s)
- Nivel 4: O(n*m) com poda — pode ser lento (1-5min para 10k orfaos)
- Nivel 5 TF-IDF: O(n*k) local — 10-30s
- Nivel 5 LLM: depende de batch size e API — 1-5min

### Carregar dois NWDs simultaneamente
- Opcao A: Append temporario (doc.AppendFile) → marca elementos por source
- Opcao B: Abrir segundo documento read-only em background
- Opcao A e preferivel pois permite comparacao no mesmo espaco de coordenadas

### Tratamento de conflitos
- Multiplos old → mesmo new: usuario escolhe qual transferir
- Propriedades com nomes iguais mas valores diferentes: mostrar diff

### Backup/Undo
- Gerar CSV com todos os atributos antes do merge
- Se possivel, usar Undo nativo do Navisworks (transacao COM)

---

## Metricas de Sucesso

- >= 95% dos elementos com ID unico sao pareados automaticamente (Nivel 1)
- >= 85% do total e pareado sem intervencao manual (Niveis 1-3)
- Tempo total < 2 min para modelos com 50k elementos (modo Automatico)
- Zero perda de atributos em elementos que existem em ambas versoes

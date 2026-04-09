# Plugin Atributos Navisworks

Plugin para Autodesk Navisworks 2026 focado em:

- leitura de propriedades do item selecionado
- gravação de atributos customizados
- seleção e gravação de nomes de `Sets`
- inspeção tabular da seleção
- colorização por modelo e por `Selection/Search Set`
- comparação entre revisões `NWD` com transferência de dados Autis

O projeto é escrito em `C#`, roda em `.NET Framework 4.8` e usa:

- `Autodesk.Navisworks.Api`
- `Autodesk.Navisworks.Interop.ComApi`
- `Autodesk.Navisworks.ComApi`
- `Windows Forms`

## Visão geral

Quando o Navisworks carrega o plugin, ele cria a aba `AWP Autis` na ribbon. A partir dela, o usuário acessa 5 fluxos principais:

1. `Read Properties`
2. `Write Properties`
3. `Selection Inspector`
4. `Colorizer`
5. `Merge NWD`

O coração do projeto é:

- UI em WinForms
- leitura de propriedades pela API gerenciada do Navisworks
- gravação de propriedades customizadas pela COM API
- empacotamento como `.bundle` Autodesk

## Estrutura do projeto

### Arquivos principais

- `AutisAtributosPlugin.cs`
  Ponto de entrada do plugin. Cria a ribbon, registra os botões e chama cada formulário.

- `AtributoService.cs`
  Serviço central de leitura, gravação e exclusão de atributos no Navisworks.

- `AutisSchema.cs`
  Define o schema atual de gravação:
  - categoria fixa: `Autis_Attributes`
  - propriedade fixa dos sets: `Autis_AWP`

- `SelectionSetCache.cs`
  Varre a árvore de `Selection Sets` e `Search Sets`, expande os itens de cada set e entrega uma coleção em cache para uso na UI e na gravação.

- `SetAssignment.cs`
  Estrutura simples usada para ligar um item do modelo ao nome de um set detectado.

### Formulários

- `GravarAtributosForm.cs`
  Tela de gravação de atributos e seleção dos sets.

- `LerAtributosForm.cs`
  Tela para leitura/exportação das propriedades do item selecionado.

- `SelectionInspectorForm.cs`
  Tabela consolidada para comparar propriedades entre vários itens selecionados.

- `ColorizerForm.cs`
  Tela para colorir por modelos ou por sets.

- `MergeForm.cs`
  Tela de análise e execução do merge entre revisões `NWD`.

- `MergeService.cs`
  Serviço de extração de fingerprint, comparação entre elementos e execução da transferência.

- `MergeModels.cs`
  Modelos de dados usados no relatório de merge.

- `MERGE_SPEC.md`
  Especificação funcional e técnica do fluxo de `Merge NWD`.

### UI compartilhada

- `UITheme.cs`
  Design system interno com cores, tipografia, espaçamento e helpers visuais.

- `CustomComponents.cs`
  Componentes visuais reutilizáveis para as telas.

### Build e instalador

- `AutisAnalytics.NavisworksAtributos.csproj`
  Configuração de build, referências Autodesk e regras de saída.

- `build_installer.bat`
  Pipeline completo para gerar o instalador.

- `installer/AutisAtributos.iss`
  Script do Inno Setup.

- `installer/staging/PackageContents.xml`
  Manifesto do bundle Autodesk usado no empacotamento.

- `installer/README_INSTALADOR.txt`
  Guia curto de build do instalador.

## Como o plugin funciona

### 1. Carga do plugin

Fluxo:

1. O Navisworks carrega `AutisAtributosPlugin`.
2. `OnLoaded()` inicia um timer.
3. Quando a ribbon do Navisworks fica disponível, o plugin cria a aba `AWP Autis`.
4. Cada botão da ribbon é ligado ao `AutisCommandHandler`.

Arquivo principal:

- `AutisAtributosPlugin.cs`

### 2. Leitura de propriedades

Fluxo:

1. O usuário seleciona um item.
2. Clica em `Read Properties`.
3. O plugin chama `AtributoService.LerPropriedades(...)`.
4. A tela `LerAtributosForm` exibe as categorias e propriedades encontradas.

Detalhe técnico:

- essa leitura usa a API gerenciada do Navisworks
- não precisa de COM para ler

### 3. Gravação de atributos

Fluxo completo:

1. O usuário seleciona um ou mais itens.
2. Clica em `Write Properties`.
3. O plugin descobre todos os sets que contêm os itens selecionados.
4. A tela `GravarAtributosForm` mostra esses sets com checkbox.
5. O usuário escolhe quais sets quer incluir.
6. O usuário pode adicionar atributos extras manualmente na grade.
7. Ao salvar, o plugin grava:
   - categoria fixa `Autis_Attributes`
   - propriedade fixa `Autis_AWP` com os nomes dos sets selecionados
   - atributos extras digitados pelo usuário

Detalhes importantes:

- os sets não viram mais várias propriedades separadas
- os nomes dos sets selecionados são consolidados em um único campo
- ao reabrir a tela, o plugin tenta ler o conteúdo já salvo em `Autis_AWP` para remarcar os sets e permitir edição posterior
- o código ainda lê formatos legados para manter compatibilidade com versões anteriores

Arquivos envolvidos:

- `AutisAtributosPlugin.cs`
- `GravarAtributosForm.cs`
- `SelectionSetCache.cs`
- `SetAssignment.cs`
- `AtributoService.cs`
- `AutisSchema.cs`

### 4. Exclusão dos atributos criados

Fluxo:

1. O usuário seleciona os itens.
2. Abre `Write Properties`.
3. Clica em `Delete Created`.
4. O plugin remove a categoria criada pelo próprio plugin.

Hoje ele remove:

- `Autis_Attributes`
- categorias legadas compatíveis, quando existirem

Arquivo:

- `AtributoService.cs`

### 5. Selection Inspector

Fluxo:

1. O usuário seleciona vários itens.
2. Clica em `Selection Inspector`.
3. O plugin coleta as propriedades de todos os itens.
4. A tela monta uma tabela comparativa.

Objetivo:

- enxergar rapidamente diferenças e similaridades entre itens selecionados

Arquivo:

- `SelectionInspectorForm.cs`

### 6. Colorizer

Fluxo:

1. O usuário abre `Colorizer`.
2. O plugin carrega automaticamente sets e modelos.
3. O usuário escolhe o modo de colorização.
4. O plugin executa a alteração visual no documento.

Observação:

- esse fluxo reutiliza `SelectionSetCache` para evitar reexpandir sets toda hora

Arquivo:

- `ColorizerForm.cs`

### 7. Merge NWD

Fluxo:

1. O usuário abre `Merge NWD`.
2. Seleciona um novo arquivo `NWD` revisado.
3. O plugin extrai fingerprints do modelo atual e do revisado.
4. O comparador executa três níveis principais:
   - `Level 1`: IDs únicos
   - `Level 2`: chave composta
   - `Level 3`: score ponderado por nome, tipo/categoria, geometria e hierarquia
5. A tela mostra:
   - pareados
   - novos
   - removidos
   - candidatos
6. O usuário pode aceitar candidatos e executar a transferência de atributos/set.

Melhorias recentes no comparador:

- `Level 1` e `Level 2` agora só fazem auto-match quando a chave é única nos dois lados
- candidatos não são mais classificados também como `New`
- o reset visual do merge limpa só as cores aplicadas pelo próprio merge
- fechar a tela sem executar o merge remove o modelo revisado anexado temporariamente

Arquivos:

- `MergeForm.cs`
- `MergeService.cs`
- `MergeModels.cs`
- `MERGE_SPEC.md`

## Persistência dos dados

### Schema atual

O schema atual foi centralizado em `AutisSchema.cs`:

- categoria principal: `Autis_Attributes`
- propriedade fixa dos sets: `Autis_AWP`

Exemplo lógico:

```text
Categoria: Autis_Attributes
  Propriedade: Autis_AWP
  Valor: AIR COOLER - TRECHO A | CABLE RACK | CASA DOS COMPRESSORES
```

Se o usuário preencher atributos extras na grade, eles também entram na categoria `Autis_Attributes`.

### Compatibilidade com versões antigas

O plugin ainda reconhece:

- categoria antiga `AWP_Autis`
- categoria antiga `Autis Analytics`
- propriedade legada `Sets`
- formato antigo onde cada set virava uma propriedade separada

Isso foi mantido para não perder usabilidade em modelos já processados.

## Passo a passo para desenvolver

### Pré-requisitos

- Windows
- Visual Studio 2022 ou `dotnet` com suporte a `.NET Framework 4.8`
- Autodesk Navisworks Manage 2026 ou ajuste da variável `NavisworksDir`

### Build local

Comando:

```powershell
dotnet build AutisAnalytics.NavisworksAtributos.csproj -c Release -p:PlatformTarget=x64 --nologo
```

Comportamento do projeto:

- `Debug`
  Sai direto para o bundle local em `%AppData%\Autodesk\ApplicationPlugins\AutisAtributos.bundle\Contents\v23\`

- `Release`
  Sai para `installer/staging/Contents/v23/`
  e depois tenta copiar para o bundle local

Observação importante:

- se o Navisworks estiver aberto, a DLL do bundle pode ficar bloqueada
- nesse caso o build compila, mas a cópia para o bundle pode falhar ou gerar aviso
- para testar a última versão no plugin carregado, feche o Navisworks antes do build

### Deploy da DLL

Passo a passo recomendado:

1. Feche o Navisworks.
2. Rode:

```powershell
dotnet build AutisAnalytics.NavisworksAtributos.csproj -c Release -p:PlatformTarget=x64 --nologo
```

3. Confirme que a DLL foi atualizada em:

```text
%AppData%\Autodesk\ApplicationPlugins\AutisAtributos.bundle\Contents\v23\
```

4. Abra o Navisworks e teste a nova versão.

## Passo a passo para testar

### Teste de leitura

1. Abra o Navisworks.
2. Selecione um item.
3. Clique em `Read Properties`.
4. Verifique se as categorias e valores aparecem corretamente.

### Teste de gravação

1. Selecione um ou mais itens.
2. Clique em `Write Properties`.
3. Marque alguns sets.
4. Adicione um atributo extra opcional.
5. Clique em `Save`.
6. Verifique se foi criada a categoria `Autis_Attributes`.
7. Verifique se `Autis_AWP` foi preenchido com os sets escolhidos.

### Teste de edição posterior

1. Grave os sets.
2. Feche a janela.
3. Abra `Write Properties` novamente com o mesmo item selecionado.
4. Confirme se os sets já salvos voltam marcados.
5. Altere a seleção e salve de novo.

### Teste de exclusão

1. Selecione itens com atributos criados pelo plugin.
2. Abra `Write Properties`.
3. Clique em `Delete Created`.
4. Confirme a exclusão.
5. Verifique se a categoria foi removida.

### Teste de merge NWD

1. Abra o modelo base no Navisworks.
2. Clique em `Merge NWD`.
3. Escolha o arquivo `NWD` revisado.
4. Aguarde a análise.
5. Revise os grupos `Matched`, `New`, `Removed` e `Candidates`.
6. Se necessário, aceite manualmente candidatos válidos.
7. Clique em `Execute Merge`.
8. Verifique no modelo revisado se os dados de `Autis_Attributes` foram transferidos.

## Passo a passo para gerar o instalador

### Opção 1: script automático

```bat
build_installer.bat
```

Ou com versão:

```bat
build_installer.bat /versao:2.0.0
```

### O que o script faz

1. verifica se `dotnet` existe
2. compila em `Release x64`
3. limpa `installer/staging/Contents/v23`
4. atualiza a versão em `installer/staging/PackageContents.xml`
5. chama o `ISCC.exe`
6. gera o instalador em `installer/output`

### Opção 2: Inno Setup manual

```powershell
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DAppVersion=2.0.0 installer\AutisAtributos.iss
```

### Arquivo gerado

Saída esperada:

```text
installer\output\AutisAtributos_v2.0.0_Setup.exe
```

## Passo a passo de release

Fluxo recomendado para fechar uma versão:

1. Feche o Navisworks.
2. Rode o build `Release`.
3. Teste no Navisworks:
   - `Read Properties`
   - `Write Properties`
   - `Selection Inspector`
   - `Colorizer`
   - `Merge NWD`
4. Gere o instalador:

```bat
build_installer.bat /versao:2.0.0
```

5. Confirme o `.exe` em `installer\output`.
6. Atualize a documentação necessária.
7. Publique no GitHub.

## Publicação e GitHub

O repositório foi preparado para publicação pública com `.gitignore` para não subir:

- `.vs`
- `obj`
- `bin`
- `.claude`
- `.vscode`
- `installer/output`
- `installer/staging/Contents/v23`
- `installer/staging/ia_config.txt`

Isso evita publicar:

- artefatos de build
- instaladores gerados
- configurações locais
- arquivos sensíveis

### Passo a passo para publicar

1. Garanta que `README.md`, `installer/README_INSTALADOR.txt` e os arquivos do plugin estejam atualizados.
2. Gere e teste a versão localmente.
3. Faça commit do código-fonte.
4. Envie para o repositório público:

```text
https://github.com/AutisAnalytics/PluginAtributosNaviswork
```

Observação:

- o instalador `.exe` é mantido como artefato local e não sobe por padrão por causa do `.gitignore`

## Onde mexer quando quiser alterar algo

### Quero mudar o nome da categoria ou da propriedade dos sets

Arquivo:

- `AutisSchema.cs`

### Quero mudar a regra de gravação/leitura dos atributos

Arquivo:

- `AtributoService.cs`

### Quero mudar a ribbon ou botões

Arquivo:

- `AutisAtributosPlugin.cs`

### Quero mudar a tela de gravação

Arquivo:

- `GravarAtributosForm.cs`

### Quero mudar a lógica de detecção dos sets

Arquivo:

- `SelectionSetCache.cs`

### Quero mudar o visual comum das telas

Arquivos:

- `UITheme.cs`
- `CustomComponents.cs`

### Quero mudar o instalador

Arquivos:

- `build_installer.bat`
- `installer/AutisAtributos.iss`
- `installer/staging/PackageContents.xml`

## Resumo rápido

Se você estiver entrando no projeto agora, a ordem ideal para entender o código é:

1. `AutisSchema.cs`
2. `AutisAtributosPlugin.cs`
3. `AtributoService.cs`
4. `SelectionSetCache.cs`
5. `GravarAtributosForm.cs`
6. `LerAtributosForm.cs`
7. `SelectionInspectorForm.cs`
8. `ColorizerForm.cs`
9. `AutisAnalytics.NavisworksAtributos.csproj`
10. `build_installer.bat`

Essa sequência já dá uma visão completa do funcionamento do plugin, do dado gravado e do processo de build/deploy.

# Markdown Notes

## Origine

Framework : dotnet-wpf v1.1.0

## Contexte métier

Application Windows de lecture et d'écriture de fichiers Markdown, conviviale.

- Éditeur de texte avec barre d'outils de mise en forme (gras, italique, titres, listes, lien, code, citation) et recherche/remplacement (Ctrl+F, Ctrl+H).
- Aperçu en direct (Markdig, GFM, WebView2), modes éditeur / aperçu / côte à côte. Diagrammes Mermaid hors ligne, liens internes `#titre`.
- Volet Plan (arbre des titres du document, clic = sélection du titre dans l'éditeur) sous l'explorateur.
- Ouverture par dialogue, glisser-déposer, argument de ligne de commande, dossier (explorateur d'arborescence), fichiers récents (10).
- Enregistrer / Enregistrer sous, confirmation des modifications non enregistrées.
- Thème clair/sombre et langue FR/EN (bascule immédiate), préférences persistantes (`%APPDATA%\MarkdownNotes\preferences.json`).
- Instance unique : un second lancement transmet son fichier à la première instance.
- Hors périmètre v1.0 : onglets, export HTML/PDF, statistiques (différés) ; enregistrement automatique (écarté).

Pas de base de données, pas de Salesforce CLI, pas de splash. Icône personnalisée ajoutée après la livraison (`app_icon/icon.png`, voir déviations). Accent `#3B6F9E` (Steel Blue assombri, contraste AA).

## Déviations par rapport au framework

- `TargetFramework` = `net10.0-windows10.0.17763.0` au lieu de `net10.0-windows` : `WebView2CompositionControl` charge `Microsoft.Windows.SDK.NET` (projection WinRT). Sans cela, la fenêtre ne s'ouvre pas. Windows 10 1809 minimum. Le composite évite le problème d'airspace entre l'aperçu et les snackbars/dialogues. Reflété dans `Directory.Build.props` et `win-x64.pubxml`.
- `.editorconfig` désactive CA1848 (LoggerMessage, surcoût injustifié pour une application de bureau), CA1863 (CompositeFormat sur des chaînes localisées lues à l'exécution), CA2007 (`ConfigureAwait` régi par `rules/threading.md` : requis dans les services, interdit dans ViewModels et Views) et CA1303 (messages de journal non destinés à l'utilisateur, libellés via `.resx`). Les champs `const` et `static readonly` gardent le PascalCase (règle de nommage dédiée). Chaque règle est commentée dans le fichier.
- `Tokens.xaml` ne définit aucun alias de brosse : `Styles.xaml` consomme directement les brosses Fluent (aucune surcharge de palette). `Tokens.Light.xaml` et `Tokens.Dark.xaml` existent pour le swap de dictionnaire mais sont vides.
- Clés `.resx` ajoutées au contrat : `Editor_Name`, `Preview_Name` (noms d'accessibilité) et touches d'accès (`_Fichier`) sur les menus de premier niveau.
- En-têtes de menu de premier niveau : gabarit compact `AppMenuTopLevelHeaderTemplate` dans `Styles.xaml`. Le gabarit WPF UI (marges fixes, environ 46 px de haut) ne tient pas dans la barre de menu de 32 px : le texte était coupé. Brosses et popup identiques à WPF UI, sans ombre ni animation. Jetons ajoutés : `MenuTopLevelPadding`, `BorderAll`.
- Dialogue des modifications non enregistrées : message dans un `TextBlock` stylé (`DialogMessageStyle`, jeton `DialogContentMinWidth`) pour élargir le dialogue. Sinon les trois boutons du pied étaient comprimés et "Ne pas enregistrer" coupé. `DialogWidth` de WPF UI n'a pas d'effet sur le dialogue simple.
- `WebViewBehavior` : une navigation du shell remplacée par une plus récente (`ConnectionAborted`) n'est plus signalée comme erreur (compteur `PendingNavigations`).
- Mermaid 11.17.2 (MIT) embarqué (`Assets/mermaid.min.js`, licence `Assets/mermaid.LICENSE.txt`, SHA-256 `581ED7D74BD9048D0E3A91363927D72EF22942D7722546B27F7CC29E35390EB8`). Bibliothèque tierce non prévue en Phase 1 : ajoutée à la demande de l'utilisateur. Injectée par `WebViewBehavior` (jamais par la page) seulement si le corps contient `class="mermaid"`, en `securityLevel: 'strict'`. Un bloc non analysable reste en texte.
- `WebViewBehavior` : `IsScriptEnabled = true` (contrat initial : scripts désactivés). Mermaid embarque DOMPurify, dont le callback Trusted Types échoue quand les scripts sont désactivés, ce qui fait échouer tous les diagrammes. Le risque reste borné : CSP `default-src 'none'` inchangée (aucun script de page ne peut s'exécuter), HTML brut désactivé dans Markdig, seuls les scripts injectés par l'hôte tournent.
- `WebViewBehavior` : les liens `#titre` du document sont interceptés dans `NavigationStarting` (URL `https://{hôte virtuel}/#id` à cause de `<base>`) et convertis en `scrollIntoView`. Toute autre navigation reste annulée. Non vérifié à l'exécution (le contenu WebView2 n'est pas accessible en UIA).
- Volet Plan : `OutlineService`, `OutlineViewModel`, `HeadingNodeViewModel`, `HeadingNode`, `OutlineView`, empilé sous l'explorateur dans la colonne gauche (séparateur horizontal `PaneRowSplitterStyle`), pas en onglets. Libellé `View_Explorer` renommé "Explorateur et plan" (l'entrée bascule toute la colonne). Clés `.resx` ajoutées : `Outline_Title`, `Outline_Empty`, `Error_OutlineFailed`. Un clic sur un titre sélectionne sa ligne dans l'éditeur (et lui donne le focus s'il est visible) et fait défiler l'aperçu jusqu'au même titre.
- Plein écran : `WebView2CompositionControl` garde la taille d'avant l'agrandissement (le chrome de fenêtre ne pousse pas la nouvelle taille au contrôleur, qui n'est pas public). `WebViewBehavior` relit le contrôleur et pose `Bounds` sur la taille arrangée à chaque changement de taille ou d'état de la fenêtre.
- Volet gauche : largeur redimensionnable (`PaneColumnBehavior`, minimum `ExplorerPaneMinWidth`). Explorateur et plan se replient chacun (`SplitPaneBehavior`) en gardant leur titre. L'état replié n'est pas enregistré dans les préférences.
- Icône d'application : `app_icon/icon.png` (source fournie par l'utilisateur) convertie en `Assets/app.ico` (16 à 256 px, entrées PNG) ; `ApplicationIcon` dans `App.Wpf.csproj` pour l'exécutable et la barre des tâches, `Resource` WPF pour l'icône de `AppTitleBarStyle`.
- `Helpers/`, `AppConfig.cs`, `Resources/` livrés au lot 1 (dépendances des services), sans changement du contrat de fichiers.

## Maintenance

- Charger le projet d'abord : `/wpf-load-project`
- Le modifier : `/wpf-add-feature` · `/wpf-fix-issue` · `/wpf-refactor-code` (chacun consigne le changement sous `[Unreleased]` dans `docs/release/CHANGELOG.md` ; la version ne bouge pas)
- Vérifier : `/wpf-run-tests`
- Publier une version : `/wpf-release` (transforme le changelog `[Unreleased]` accumulé en version datée et incrémente le numéro)

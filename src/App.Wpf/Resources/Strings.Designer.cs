#nullable enable
using System.Globalization;
using System.Resources;

namespace MarkdownNotes.Resources;

/// <summary>Strongly typed accessors for <c>Strings.resx</c> (FR neutral) and <c>Strings.en.resx</c>.</summary>
internal static class Strings
{
    private static ResourceManager? _resourceManager;

    /// <summary>Gets the resource manager of the string tables.</summary>
    internal static ResourceManager ResourceManager => _resourceManager ??= new ResourceManager("MarkdownNotes.Resources.Strings", typeof(Strings).Assembly);

    /// <summary>Gets or sets the culture used to resolve strings; null follows the current UI culture.</summary>
    internal static CultureInfo? Culture { get; set; }

    /// <summary>Gets the string for <c>Menu_File</c> (neutral value: Fichier).</summary>
    internal static string Menu_File => ResourceManager.GetString(nameof(Menu_File), Culture) ?? nameof(Menu_File);

    /// <summary>Gets the string for <c>Menu_File_New</c> (neutral value: Nouveau).</summary>
    internal static string Menu_File_New => ResourceManager.GetString(nameof(Menu_File_New), Culture) ?? nameof(Menu_File_New);

    /// <summary>Gets the string for <c>Menu_File_Open</c> (neutral value: Ouvrir...).</summary>
    internal static string Menu_File_Open => ResourceManager.GetString(nameof(Menu_File_Open), Culture) ?? nameof(Menu_File_Open);

    /// <summary>Gets the string for <c>Menu_File_OpenFolder</c> (neutral value: Ouvrir un dossier...).</summary>
    internal static string Menu_File_OpenFolder => ResourceManager.GetString(nameof(Menu_File_OpenFolder), Culture) ?? nameof(Menu_File_OpenFolder);

    /// <summary>Gets the string for <c>Menu_File_Recent</c> (neutral value: Fichiers récents).</summary>
    internal static string Menu_File_Recent => ResourceManager.GetString(nameof(Menu_File_Recent), Culture) ?? nameof(Menu_File_Recent);

    /// <summary>Gets the string for <c>Menu_File_Recent_None</c> (neutral value: (aucun)).</summary>
    internal static string Menu_File_Recent_None => ResourceManager.GetString(nameof(Menu_File_Recent_None), Culture) ?? nameof(Menu_File_Recent_None);

    /// <summary>Gets the string for <c>Menu_File_Save</c> (neutral value: Enregistrer).</summary>
    internal static string Menu_File_Save => ResourceManager.GetString(nameof(Menu_File_Save), Culture) ?? nameof(Menu_File_Save);

    /// <summary>Gets the string for <c>Menu_File_SaveAs</c> (neutral value: Enregistrer sous...).</summary>
    internal static string Menu_File_SaveAs => ResourceManager.GetString(nameof(Menu_File_SaveAs), Culture) ?? nameof(Menu_File_SaveAs);

    /// <summary>Gets the string for <c>Menu_File_Exit</c> (neutral value: Quitter).</summary>
    internal static string Menu_File_Exit => ResourceManager.GetString(nameof(Menu_File_Exit), Culture) ?? nameof(Menu_File_Exit);

    /// <summary>Gets the string for <c>Menu_Edit</c> (neutral value: Édition).</summary>
    internal static string Menu_Edit => ResourceManager.GetString(nameof(Menu_Edit), Culture) ?? nameof(Menu_Edit);

    /// <summary>Gets the string for <c>Menu_Edit_Undo</c> (neutral value: Annuler).</summary>
    internal static string Menu_Edit_Undo => ResourceManager.GetString(nameof(Menu_Edit_Undo), Culture) ?? nameof(Menu_Edit_Undo);

    /// <summary>Gets the string for <c>Menu_Edit_Redo</c> (neutral value: Rétablir).</summary>
    internal static string Menu_Edit_Redo => ResourceManager.GetString(nameof(Menu_Edit_Redo), Culture) ?? nameof(Menu_Edit_Redo);

    /// <summary>Gets the string for <c>Menu_Edit_Cut</c> (neutral value: Couper).</summary>
    internal static string Menu_Edit_Cut => ResourceManager.GetString(nameof(Menu_Edit_Cut), Culture) ?? nameof(Menu_Edit_Cut);

    /// <summary>Gets the string for <c>Menu_Edit_Copy</c> (neutral value: Copier).</summary>
    internal static string Menu_Edit_Copy => ResourceManager.GetString(nameof(Menu_Edit_Copy), Culture) ?? nameof(Menu_Edit_Copy);

    /// <summary>Gets the string for <c>Menu_Edit_Paste</c> (neutral value: Coller).</summary>
    internal static string Menu_Edit_Paste => ResourceManager.GetString(nameof(Menu_Edit_Paste), Culture) ?? nameof(Menu_Edit_Paste);

    /// <summary>Gets the string for <c>Menu_Edit_Find</c> (neutral value: Rechercher).</summary>
    internal static string Menu_Edit_Find => ResourceManager.GetString(nameof(Menu_Edit_Find), Culture) ?? nameof(Menu_Edit_Find);

    /// <summary>Gets the string for <c>Menu_Edit_Replace</c> (neutral value: Remplacer).</summary>
    internal static string Menu_Edit_Replace => ResourceManager.GetString(nameof(Menu_Edit_Replace), Culture) ?? nameof(Menu_Edit_Replace);

    /// <summary>Gets the string for <c>Menu_Format</c> (neutral value: Format).</summary>
    internal static string Menu_Format => ResourceManager.GetString(nameof(Menu_Format), Culture) ?? nameof(Menu_Format);

    /// <summary>Gets the string for <c>Format_Bold</c> (neutral value: Gras).</summary>
    internal static string Format_Bold => ResourceManager.GetString(nameof(Format_Bold), Culture) ?? nameof(Format_Bold);

    /// <summary>Gets the string for <c>Format_Italic</c> (neutral value: Italique).</summary>
    internal static string Format_Italic => ResourceManager.GetString(nameof(Format_Italic), Culture) ?? nameof(Format_Italic);

    /// <summary>Gets the string for <c>Format_Heading1</c> (neutral value: Titre 1).</summary>
    internal static string Format_Heading1 => ResourceManager.GetString(nameof(Format_Heading1), Culture) ?? nameof(Format_Heading1);

    /// <summary>Gets the string for <c>Format_Heading2</c> (neutral value: Titre 2).</summary>
    internal static string Format_Heading2 => ResourceManager.GetString(nameof(Format_Heading2), Culture) ?? nameof(Format_Heading2);

    /// <summary>Gets the string for <c>Format_Heading3</c> (neutral value: Titre 3).</summary>
    internal static string Format_Heading3 => ResourceManager.GetString(nameof(Format_Heading3), Culture) ?? nameof(Format_Heading3);

    /// <summary>Gets the string for <c>Format_BulletList</c> (neutral value: Liste à puces).</summary>
    internal static string Format_BulletList => ResourceManager.GetString(nameof(Format_BulletList), Culture) ?? nameof(Format_BulletList);

    /// <summary>Gets the string for <c>Format_NumberedList</c> (neutral value: Liste numérotée).</summary>
    internal static string Format_NumberedList => ResourceManager.GetString(nameof(Format_NumberedList), Culture) ?? nameof(Format_NumberedList);

    /// <summary>Gets the string for <c>Format_Link</c> (neutral value: Lien).</summary>
    internal static string Format_Link => ResourceManager.GetString(nameof(Format_Link), Culture) ?? nameof(Format_Link);

    /// <summary>Gets the string for <c>Format_Code</c> (neutral value: Code).</summary>
    internal static string Format_Code => ResourceManager.GetString(nameof(Format_Code), Culture) ?? nameof(Format_Code);

    /// <summary>Gets the string for <c>Format_Quote</c> (neutral value: Citation).</summary>
    internal static string Format_Quote => ResourceManager.GetString(nameof(Format_Quote), Culture) ?? nameof(Format_Quote);

    /// <summary>Gets the string for <c>Menu_View</c> (neutral value: Affichage).</summary>
    internal static string Menu_View => ResourceManager.GetString(nameof(Menu_View), Culture) ?? nameof(Menu_View);

    /// <summary>Gets the string for <c>View_Mode_Split</c> (neutral value: Éditeur et aperçu).</summary>
    internal static string View_Mode_Split => ResourceManager.GetString(nameof(View_Mode_Split), Culture) ?? nameof(View_Mode_Split);

    /// <summary>Gets the string for <c>View_Mode_Editor</c> (neutral value: Éditeur seul).</summary>
    internal static string View_Mode_Editor => ResourceManager.GetString(nameof(View_Mode_Editor), Culture) ?? nameof(View_Mode_Editor);

    /// <summary>Gets the string for <c>View_Mode_Preview</c> (neutral value: Aperçu seul).</summary>
    internal static string View_Mode_Preview => ResourceManager.GetString(nameof(View_Mode_Preview), Culture) ?? nameof(View_Mode_Preview);

    /// <summary>Gets the string for <c>View_Explorer</c> (neutral value: Explorateur et plan).</summary>
    internal static string View_Explorer => ResourceManager.GetString(nameof(View_Explorer), Culture) ?? nameof(View_Explorer);

    /// <summary>Gets the string for <c>View_Theme</c> (neutral value: Thème).</summary>
    internal static string View_Theme => ResourceManager.GetString(nameof(View_Theme), Culture) ?? nameof(View_Theme);

    /// <summary>Gets the string for <c>View_Theme_Light</c> (neutral value: Clair).</summary>
    internal static string View_Theme_Light => ResourceManager.GetString(nameof(View_Theme_Light), Culture) ?? nameof(View_Theme_Light);

    /// <summary>Gets the string for <c>View_Theme_Dark</c> (neutral value: Sombre).</summary>
    internal static string View_Theme_Dark => ResourceManager.GetString(nameof(View_Theme_Dark), Culture) ?? nameof(View_Theme_Dark);

    /// <summary>Gets the string for <c>Menu_Language</c> (neutral value: Langue).</summary>
    internal static string Menu_Language => ResourceManager.GetString(nameof(Menu_Language), Culture) ?? nameof(Menu_Language);

    /// <summary>Gets the string for <c>Language_French</c> (neutral value: Français).</summary>
    internal static string Language_French => ResourceManager.GetString(nameof(Language_French), Culture) ?? nameof(Language_French);

    /// <summary>Gets the string for <c>Language_English</c> (neutral value: English).</summary>
    internal static string Language_English => ResourceManager.GetString(nameof(Language_English), Culture) ?? nameof(Language_English);

    /// <summary>Gets the string for <c>Explorer_Title</c> (neutral value: Explorateur).</summary>
    internal static string Explorer_Title => ResourceManager.GetString(nameof(Explorer_Title), Culture) ?? nameof(Explorer_Title);

    /// <summary>Gets the string for <c>Explorer_Refresh</c> (neutral value: Actualiser).</summary>
    internal static string Explorer_Refresh => ResourceManager.GetString(nameof(Explorer_Refresh), Culture) ?? nameof(Explorer_Refresh);

    /// <summary>Gets the string for <c>Explorer_Empty</c> (neutral value: Aucun dossier ouvert).</summary>
    internal static string Explorer_Empty => ResourceManager.GetString(nameof(Explorer_Empty), Culture) ?? nameof(Explorer_Empty);

    /// <summary>Gets the string for <c>Outline_Title</c> (neutral value: Plan).</summary>
    internal static string Outline_Title => ResourceManager.GetString(nameof(Outline_Title), Culture) ?? nameof(Outline_Title);

    /// <summary>Gets the string for <c>Outline_Empty</c> (neutral value: Aucun titre dans le document).</summary>
    internal static string Outline_Empty => ResourceManager.GetString(nameof(Outline_Empty), Culture) ?? nameof(Outline_Empty);

    /// <summary>Gets the string for <c>Pane_Collapse</c> (neutral value: Réduire).</summary>
    internal static string Pane_Collapse => ResourceManager.GetString(nameof(Pane_Collapse), Culture) ?? nameof(Pane_Collapse);

    /// <summary>Gets the string for <c>Pane_Expand</c> (neutral value: Développer).</summary>
    internal static string Pane_Expand => ResourceManager.GetString(nameof(Pane_Expand), Culture) ?? nameof(Pane_Expand);

    /// <summary>Gets the string for <c>Explorer_OpenFolder</c> (neutral value: Ouvrir un dossier).</summary>
    internal static string Explorer_OpenFolder => ResourceManager.GetString(nameof(Explorer_OpenFolder), Culture) ?? nameof(Explorer_OpenFolder);

    /// <summary>Gets the string for <c>Find_Query</c> (neutral value: Rechercher).</summary>
    internal static string Find_Query => ResourceManager.GetString(nameof(Find_Query), Culture) ?? nameof(Find_Query);

    /// <summary>Gets the string for <c>Find_Replacement</c> (neutral value: Remplacer par).</summary>
    internal static string Find_Replacement => ResourceManager.GetString(nameof(Find_Replacement), Culture) ?? nameof(Find_Replacement);

    /// <summary>Gets the string for <c>Find_MatchCase</c> (neutral value: Respecter la casse).</summary>
    internal static string Find_MatchCase => ResourceManager.GetString(nameof(Find_MatchCase), Culture) ?? nameof(Find_MatchCase);

    /// <summary>Gets the string for <c>Find_Next</c> (neutral value: Suivant).</summary>
    internal static string Find_Next => ResourceManager.GetString(nameof(Find_Next), Culture) ?? nameof(Find_Next);

    /// <summary>Gets the string for <c>Find_Previous</c> (neutral value: Précédent).</summary>
    internal static string Find_Previous => ResourceManager.GetString(nameof(Find_Previous), Culture) ?? nameof(Find_Previous);

    /// <summary>Gets the string for <c>Find_Replace</c> (neutral value: Remplacer).</summary>
    internal static string Find_Replace => ResourceManager.GetString(nameof(Find_Replace), Culture) ?? nameof(Find_Replace);

    /// <summary>Gets the string for <c>Find_ReplaceAll</c> (neutral value: Tout remplacer).</summary>
    internal static string Find_ReplaceAll => ResourceManager.GetString(nameof(Find_ReplaceAll), Culture) ?? nameof(Find_ReplaceAll);

    /// <summary>Gets the string for <c>Find_Close</c> (neutral value: Fermer).</summary>
    internal static string Find_Close => ResourceManager.GetString(nameof(Find_Close), Culture) ?? nameof(Find_Close);

    /// <summary>Gets the string for <c>Find_NoMatch</c> (neutral value: Aucun résultat).</summary>
    internal static string Find_NoMatch => ResourceManager.GetString(nameof(Find_NoMatch), Culture) ?? nameof(Find_NoMatch);

    /// <summary>Gets the string for <c>Find_Replaced</c> (neutral value: {0} remplacement(s)).</summary>
    internal static string Find_Replaced => ResourceManager.GetString(nameof(Find_Replaced), Culture) ?? nameof(Find_Replaced);

    /// <summary>Gets the string for <c>Status_Ready</c> (neutral value: Prêt).</summary>
    internal static string Status_Ready => ResourceManager.GetString(nameof(Status_Ready), Culture) ?? nameof(Status_Ready);

    /// <summary>Gets the string for <c>Status_Modified</c> (neutral value: Modifié).</summary>
    internal static string Status_Modified => ResourceManager.GetString(nameof(Status_Modified), Culture) ?? nameof(Status_Modified);

    /// <summary>Gets the string for <c>Status_LineColumn</c> (neutral value: Ln {0}, Col {1}).</summary>
    internal static string Status_LineColumn => ResourceManager.GetString(nameof(Status_LineColumn), Culture) ?? nameof(Status_LineColumn);

    /// <summary>Gets the string for <c>Status_Untitled</c> (neutral value: Sans titre).</summary>
    internal static string Status_Untitled => ResourceManager.GetString(nameof(Status_Untitled), Culture) ?? nameof(Status_Untitled);

    /// <summary>Gets the string for <c>Status_Opened</c> (neutral value: {0} ouvert).</summary>
    internal static string Status_Opened => ResourceManager.GetString(nameof(Status_Opened), Culture) ?? nameof(Status_Opened);

    /// <summary>Gets the string for <c>Status_Saved</c> (neutral value: {0} enregistré).</summary>
    internal static string Status_Saved => ResourceManager.GetString(nameof(Status_Saved), Culture) ?? nameof(Status_Saved);

    /// <summary>Gets the string for <c>Unsaved_Title</c> (neutral value: Modifications non enregistrées).</summary>
    internal static string Unsaved_Title => ResourceManager.GetString(nameof(Unsaved_Title), Culture) ?? nameof(Unsaved_Title);

    /// <summary>Gets the string for <c>Unsaved_Message</c> (neutral value: Enregistrer les modifications de {0} ?).</summary>
    internal static string Unsaved_Message => ResourceManager.GetString(nameof(Unsaved_Message), Culture) ?? nameof(Unsaved_Message);

    /// <summary>Gets the string for <c>Unsaved_Save</c> (neutral value: Enregistrer).</summary>
    internal static string Unsaved_Save => ResourceManager.GetString(nameof(Unsaved_Save), Culture) ?? nameof(Unsaved_Save);

    /// <summary>Gets the string for <c>Unsaved_Discard</c> (neutral value: Ne pas enregistrer).</summary>
    internal static string Unsaved_Discard => ResourceManager.GetString(nameof(Unsaved_Discard), Culture) ?? nameof(Unsaved_Discard);

    /// <summary>Gets the string for <c>Unsaved_Cancel</c> (neutral value: Annuler).</summary>
    internal static string Unsaved_Cancel => ResourceManager.GetString(nameof(Unsaved_Cancel), Culture) ?? nameof(Unsaved_Cancel);

    /// <summary>Gets the string for <c>Dialog_OpenTitle</c> (neutral value: Ouvrir un document).</summary>
    internal static string Dialog_OpenTitle => ResourceManager.GetString(nameof(Dialog_OpenTitle), Culture) ?? nameof(Dialog_OpenTitle);

    /// <summary>Gets the string for <c>Dialog_SaveTitle</c> (neutral value: Enregistrer le document).</summary>
    internal static string Dialog_SaveTitle => ResourceManager.GetString(nameof(Dialog_SaveTitle), Culture) ?? nameof(Dialog_SaveTitle);

    /// <summary>Gets the string for <c>Dialog_FolderTitle</c> (neutral value: Ouvrir un dossier).</summary>
    internal static string Dialog_FolderTitle => ResourceManager.GetString(nameof(Dialog_FolderTitle), Culture) ?? nameof(Dialog_FolderTitle);

    /// <summary>Gets the string for <c>Dialog_Filter_Markdown</c> (neutral value: Documents Markdown (*.md;*.markdown;*.txt)|*.md;*.markdown;*.txt|Tous les fichiers (*.*)|*.*).</summary>
    internal static string Dialog_Filter_Markdown => ResourceManager.GetString(nameof(Dialog_Filter_Markdown), Culture) ?? nameof(Dialog_Filter_Markdown);

    /// <summary>Gets the string for <c>Feedback_Success</c> (neutral value: Terminé).</summary>
    internal static string Feedback_Success => ResourceManager.GetString(nameof(Feedback_Success), Culture) ?? nameof(Feedback_Success);

    /// <summary>Gets the string for <c>Feedback_Info</c> (neutral value: Information).</summary>
    internal static string Feedback_Info => ResourceManager.GetString(nameof(Feedback_Info), Culture) ?? nameof(Feedback_Info);

    /// <summary>Gets the string for <c>Feedback_Warning</c> (neutral value: Attention).</summary>
    internal static string Feedback_Warning => ResourceManager.GetString(nameof(Feedback_Warning), Culture) ?? nameof(Feedback_Warning);

    /// <summary>Gets the string for <c>Feedback_Danger</c> (neutral value: Erreur).</summary>
    internal static string Feedback_Danger => ResourceManager.GetString(nameof(Feedback_Danger), Culture) ?? nameof(Feedback_Danger);

    /// <summary>Gets the string for <c>Error_Unexpected</c> (neutral value: Une erreur inattendue s&apos;est produite).</summary>
    internal static string Error_Unexpected => ResourceManager.GetString(nameof(Error_Unexpected), Culture) ?? nameof(Error_Unexpected);

    /// <summary>Gets the string for <c>Error_FileNotFound</c> (neutral value: Fichier introuvable).</summary>
    internal static string Error_FileNotFound => ResourceManager.GetString(nameof(Error_FileNotFound), Culture) ?? nameof(Error_FileNotFound);

    /// <summary>Gets the string for <c>Error_FileTooLarge</c> (neutral value: Document trop volumineux (10 Mo maximum)).</summary>
    internal static string Error_FileTooLarge => ResourceManager.GetString(nameof(Error_FileTooLarge), Culture) ?? nameof(Error_FileTooLarge);

    /// <summary>Gets the string for <c>Error_InvalidPath</c> (neutral value: Chemin de fichier non valide).</summary>
    internal static string Error_InvalidPath => ResourceManager.GetString(nameof(Error_InvalidPath), Culture) ?? nameof(Error_InvalidPath);

    /// <summary>Gets the string for <c>Error_ReadFailed</c> (neutral value: Impossible de lire le fichier).</summary>
    internal static string Error_ReadFailed => ResourceManager.GetString(nameof(Error_ReadFailed), Culture) ?? nameof(Error_ReadFailed);

    /// <summary>Gets the string for <c>Error_WriteFailed</c> (neutral value: Impossible d&apos;enregistrer le fichier).</summary>
    internal static string Error_WriteFailed => ResourceManager.GetString(nameof(Error_WriteFailed), Culture) ?? nameof(Error_WriteFailed);

    /// <summary>Gets the string for <c>Error_FolderReadFailed</c> (neutral value: Impossible de lire le dossier).</summary>
    internal static string Error_FolderReadFailed => ResourceManager.GetString(nameof(Error_FolderReadFailed), Culture) ?? nameof(Error_FolderReadFailed);

    /// <summary>Gets the string for <c>Error_PreferencesLoad</c> (neutral value: Préférences illisibles, valeurs par défaut utilisées).</summary>
    internal static string Error_PreferencesLoad => ResourceManager.GetString(nameof(Error_PreferencesLoad), Culture) ?? nameof(Error_PreferencesLoad);

    /// <summary>Gets the string for <c>Error_PreferencesSave</c> (neutral value: Impossible d&apos;enregistrer les préférences).</summary>
    internal static string Error_PreferencesSave => ResourceManager.GetString(nameof(Error_PreferencesSave), Culture) ?? nameof(Error_PreferencesSave);

    /// <summary>Gets the string for <c>Error_PreviewTooLarge</c> (neutral value: Document trop volumineux pour l&apos;aperçu).</summary>
    internal static string Error_PreviewTooLarge => ResourceManager.GetString(nameof(Error_PreviewTooLarge), Culture) ?? nameof(Error_PreviewTooLarge);

    /// <summary>Gets the string for <c>Error_PreviewFailed</c> (neutral value: Impossible d&apos;afficher l&apos;aperçu).</summary>
    internal static string Error_PreviewFailed => ResourceManager.GetString(nameof(Error_PreviewFailed), Culture) ?? nameof(Error_PreviewFailed);

    /// <summary>Gets the string for <c>Error_OutlineFailed</c> (neutral value: Impossible de construire le plan).</summary>
    internal static string Error_OutlineFailed => ResourceManager.GetString(nameof(Error_OutlineFailed), Culture) ?? nameof(Error_OutlineFailed);

    /// <summary>Gets the string for <c>Warning_WebViewMissing</c> (neutral value: Le runtime WebView2 est absent, l&apos;aperçu est désactivé).</summary>
    internal static string Warning_WebViewMissing => ResourceManager.GetString(nameof(Warning_WebViewMissing), Culture) ?? nameof(Warning_WebViewMissing);

    /// <summary>Gets the string for <c>Preview_Unavailable</c> (neutral value: Aperçu indisponible : installez le runtime Microsoft Edge WebView2.).</summary>
    internal static string Preview_Unavailable => ResourceManager.GetString(nameof(Preview_Unavailable), Culture) ?? nameof(Preview_Unavailable);

    /// <summary>Gets the string for <c>Editor_Name</c> (neutral value: Éditeur Markdown).</summary>
    internal static string Editor_Name => ResourceManager.GetString(nameof(Editor_Name), Culture) ?? nameof(Editor_Name);

    /// <summary>Gets the string for <c>Preview_Name</c> (neutral value: Aperçu du document).</summary>
    internal static string Preview_Name => ResourceManager.GetString(nameof(Preview_Name), Culture) ?? nameof(Preview_Name);
}

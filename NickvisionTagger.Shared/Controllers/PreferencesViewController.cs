using NickvisionTagger.Shared.Models;

namespace NickvisionTagger.Shared.Controllers;

/// <summary>
/// A controller for a PreferencesView
/// </summary>
public class PreferencesViewController
{
    /// <summary>
    /// Gets the AppInfo object
    /// </summary>
    public AppInfo AppInfo => AppInfo.Current;
    /// <summary>
    /// The link to get a new AcoustId user API key
    /// </summary>
    public string AcoustIdUserAPIKeyLink => "https://acoustid.org/api-key";

    /// <summary>
    /// Constructs a PreferencesViewController
    /// </summary>
    internal PreferencesViewController()
    {

    }

    /// <summary>
    /// The preferred theme of the application
    /// </summary>
    public Theme Theme
    {
        get => Configuration.Current.Theme;

        set => Configuration.Current.Theme = value;
    }

    /// <summary>
    /// Whether or not to remember the last opened folder
    /// </summary>
    public bool RememberLastOpenedFolder
    {
        get => Configuration.Current.RememberLastOpenedFolder;

        set => Configuration.Current.RememberLastOpenedFolder = value;
    }

    /// <summary>
    /// Whether or not to scan subfolders for music
    /// </summary>
    public bool IncludeSubfolders
    {
        get => Configuration.Current.IncludeSubfolders;

        set => Configuration.Current.IncludeSubfolders = value;
    }

    /// <summary>
    /// What to sort files in a music folder by
    /// </summary>
    public SortBy SortFilesBy
    {
        get => Configuration.Current.SortFilesBy;

        set => Configuration.Current.SortFilesBy = value;
    }

    /// <summary>
    /// Whether or not to preserve (not change) a file's modification timestamp
    /// </summary>
    public bool PreserveModificationTimestamp
    {
        get => Configuration.Current.PreserveModificationTimestamp;

        set => Configuration.Current.PreserveModificationTimestamp = value;
    }

    /// <summary>
    /// Whether or not to overwrite a tag's existing data with data from MusicBrainz
    /// </summary>
    public bool OverwriteTagWithMusicBrainz
    {
        get => Configuration.Current.OverwriteTagWithMusicBrainz;

        set => Configuration.Current.OverwriteTagWithMusicBrainz = value;
    }

    /// <summary>
    /// Whether or not to overwrite a tag's existing album art with album art from MusicBrainz
    /// </summary>
    public bool OverwriteAlbumArtWithMusicBrainz
    {
        get => Configuration.Current.OverwriteAlbumArtWithMusicBrainz;

        set => Configuration.Current.OverwriteAlbumArtWithMusicBrainz = value;
    }

    /// <summary>
    /// The user's AcoustId API Key
    /// </summary>
    public string AcoustIdUserAPIKey
    {
        get => Configuration.Current.AcoustIdUserAPIKey;

        set => Configuration.Current.AcoustIdUserAPIKey = value;
    }

    /// <summary>
    /// Saves the configuration to disk
    /// </summary>
    public void SaveConfiguration() => Configuration.Current.Save();
}

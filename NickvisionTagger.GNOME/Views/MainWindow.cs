using NickvisionTagger.GNOME.Controls;
using NickvisionTagger.GNOME.Helpers;
using NickvisionTagger.Shared.Controllers;
using NickvisionTagger.Shared.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NickvisionTagger.Shared.Models;
using static NickvisionTagger.Shared.Helpers.Gettext;

namespace NickvisionTagger.GNOME.Views;

/// <summary>
/// The MainWindow for the application
/// </summary>
public partial class MainWindow : Adw.ApplicationWindow
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GLibList
    {
        public nint data;
        public GLibList* next;
        public GLibList* prev;
    }

    private delegate bool GSourceFunc(nint data);
    private delegate void GAsyncReadyCallback(nint source, nint res, nint user_data);

    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void g_object_unref(nint obj);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void g_main_context_invoke(nint context, GSourceFunc function, nint data);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint g_main_context_default();
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void g_main_context_iteration(nint context, [MarshalAs(UnmanagedType.I1)] bool may_block);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial string g_file_get_path(nint file);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint gtk_file_dialog_new();
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void gtk_file_dialog_set_title(nint dialog, string title);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void gtk_file_dialog_set_filters(nint dialog, nint filters);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void gtk_file_dialog_open(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint gtk_file_dialog_open_finish(nint dialog, nint result, nint error);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void gtk_file_dialog_select_folder(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint gtk_file_dialog_select_folder_finish(nint dialog, nint result, nint error);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint g_file_new_for_path(string path);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint g_file_icon_new(nint gfile);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void g_notification_set_icon(nint notification, nint icon);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial GLibList* gtk_list_box_get_selected_rows(nint box);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int gtk_list_box_row_get_index(nint row);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint gdk_pixbuf_loader_new();
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void gdk_pixbuf_loader_write(nint loader, [MarshalAs(UnmanagedType.LPArray)] byte[] buf, uint count, nint error);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint gdk_pixbuf_loader_get_pixbuf(nint loader);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void gtk_image_set_from_pixbuf(nint image, nint pixbuf);
    [LibraryImport("libadwaita-1.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void gdk_pixbuf_loader_close(nint loader, nint error);

    private readonly MainWindowController _controller;
    private readonly Adw.Application _application;
    private readonly Gtk.DropTarget _dropTarget;
    private List<Adw.ActionRow> _listMusicFilesRows;
    private bool _isSelectionOccuring;
    private readonly GSourceFunc _musicFolderUpdatedFunc;
    private readonly GSourceFunc _musicFileSaveStatesChangedFunc;
    private readonly GSourceFunc _selectedMusicFilesPropertiesChangedFunc;
    private GAsyncReadyCallback? _openCallback;

    [Gtk.Connect] private readonly Adw.HeaderBar _headerBar;
    [Gtk.Connect] private readonly Adw.WindowTitle _title;
    [Gtk.Connect] private readonly Gtk.Button _openFolderButton;
    [Gtk.Connect] private readonly Gtk.Button _reloadFolderButton;
    [Gtk.Connect] private readonly Gtk.Separator _headerEndSeparator;
    [Gtk.Connect] private readonly Gtk.Button _applyButton;
    [Gtk.Connect] private readonly Gtk.MenuButton _tagActionsButton;
    [Gtk.Connect] private readonly Gtk.MenuButton _webServicesButton;
    [Gtk.Connect] private readonly Adw.ToastOverlay _toastOverlay;
    [Gtk.Connect] private readonly Adw.ViewStack _viewStack;
    [Gtk.Connect] private readonly Gtk.Label _loadingLabel;
    [Gtk.Connect] private readonly Adw.ViewStack _filesViewStack;
    [Gtk.Connect] private readonly Gtk.SearchEntry _musicFilesSearch;
    [Gtk.Connect] private readonly Gtk.Button _advancedSearchInfoButton;
    [Gtk.Connect] private readonly Gtk.ListBox _listMusicFiles;
    [Gtk.Connect] private readonly Adw.ViewStack _artViewStack;
    [Gtk.Connect] private readonly Gtk.Button _noAlbumArtButton;
    [Gtk.Connect] private readonly Gtk.Button _albumArtButton;
    [Gtk.Connect] private readonly Gtk.Image _albumArtImage;
    [Gtk.Connect] private readonly Gtk.Button _keepAlbumArtButton;
    [Gtk.Connect] private readonly Adw.EntryRow _filenameRow;
    [Gtk.Connect] private readonly Adw.EntryRow _titleRow;
    [Gtk.Connect] private readonly Adw.EntryRow _artistRow;
    [Gtk.Connect] private readonly Adw.EntryRow _albumRow;
    [Gtk.Connect] private readonly Adw.EntryRow _yearRow;
    [Gtk.Connect] private readonly Adw.EntryRow _trackRow;
    [Gtk.Connect] private readonly Adw.EntryRow _albumArtistRow;
    [Gtk.Connect] private readonly Adw.EntryRow _genreRow;
    [Gtk.Connect] private readonly Adw.EntryRow _commentRow;
    [Gtk.Connect] private readonly Adw.EntryRow _durationRow;
    [Gtk.Connect] private readonly Adw.EntryRow _fingerprintRow;
    [Gtk.Connect] private readonly Adw.EntryRow _fileSizeRow;

    private MainWindow(Gtk.Builder builder, MainWindowController controller, Adw.Application application) : base(builder.GetPointer("_root"), false)
    {
        //Window Settings
        _controller = controller;
        _application = application;
        _openCallback = null;
        _listMusicFilesRows = new List<Adw.ActionRow>();
        _isSelectionOccuring = false;
        _musicFolderUpdatedFunc =  MusicFolderUpdated;
        _musicFileSaveStatesChangedFunc = (x) => MusicFileSaveStatesChanged();
        _selectedMusicFilesPropertiesChangedFunc = (x) => SelectedMusicFilesPropertiesChanged();
        SetDefaultSize(800, 600);
        SetTitle(_controller.AppInfo.ShortName);
        SetIconName(_controller.AppInfo.ID);
        if (_controller.IsDevVersion)
        {
            AddCssClass("devel");
        }
        //Build UI
        builder.Connect(this);
        _title.SetTitle(_controller.AppInfo.ShortName);
        _musicFilesSearch.OnSearchChanged += SearchChanged;
        _advancedSearchInfoButton.OnClicked += AdvancedSearchInfo;
        _listMusicFiles.OnSelectedRowsChanged += ListMusicFiles_SelectionChanged;
        _filenameRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        _titleRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        _artistRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        _albumRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        _yearRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        _trackRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        _albumArtistRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        _genreRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        _commentRow.OnNotify += (sender, e) =>
        {
            if(e.Pspec.GetName() == "text")
            {
                TagPropertyChanged();
            }
        };
        //Register Events
        OnCloseRequest += OnCloseRequested;
        _controller.NotificationSent += NotificationSent;
        _controller.ShellNotificationSent += ShellNotificationSent;
        _controller.MusicFolderUpdated += (sender, e) => g_main_context_invoke(0, _musicFolderUpdatedFunc, (IntPtr)GCHandle.Alloc(e));
        _controller.MusicFileSaveStatesChanged += (sender, e) => g_main_context_invoke(0, _musicFileSaveStatesChangedFunc, 0);
        _controller.SelectedMusicFilesPropertiesChanged += (sender, e) => g_main_context_invoke(0, _selectedMusicFilesPropertiesChangedFunc, 0);
        //Open Folder Action
        var actOpenFolder = Gio.SimpleAction.New("openFolder", null);
        actOpenFolder.OnActivate += OpenFolder;
        AddAction(actOpenFolder);
        application.SetAccelsForAction("win.openFolder", new string[] { "<Ctrl>O" });
        //Close Folder Action
        var actCloseFolder = Gio.SimpleAction.New("closeFolder", null);
        actCloseFolder.OnActivate += (sender, e) => _controller.CloseFolder();
        AddAction(actCloseFolder);
        application.SetAccelsForAction("win.closeFolder", new string[] { "<Ctrl>W" });
        //Reload Folder Action
        var actReloadFolder = Gio.SimpleAction.New("reloadFolder", null);
        actReloadFolder.OnActivate += ReloadFolder;
        AddAction(actReloadFolder);
        application.SetAccelsForAction("win.reloadFolder", new string[] { "F5" });
        //Apply Action
        var actApply = Gio.SimpleAction.New("apply", null);
        actApply.OnActivate += Apply;
        AddAction(actApply);
        application.SetAccelsForAction("win.apply", new string[] { "<Ctrl>S" });
        //Discard Unapplied Changes Action
        var actDiscard = Gio.SimpleAction.New("discardUnappliedChanges", null);
        actDiscard.OnActivate += DiscardUnappliedChanges;
        AddAction(actDiscard);
        application.SetAccelsForAction("win.discardUnappliedChanges", new string[] { "<Ctrl>Z" });
        //Delete Tags Action
        var actDeleteTags = Gio.SimpleAction.New("deleteTags", null);
        actDeleteTags.OnActivate += DeleteTags;
        AddAction(actDeleteTags);
        application.SetAccelsForAction("win.deleteTags", new string[] { "<Shift>Delete" });
        //Convert Filename To Tag Action
        var actFTT = Gio.SimpleAction.New("filenameToTag", null);
        actFTT.OnActivate += FilenameToTag;
        AddAction(actFTT);
        application.SetAccelsForAction("win.filenameToTag", new string[] { "<Ctrl>F" });
        //Convert Tag To Filename Action
        var actTTF = Gio.SimpleAction.New("tagToFilename", null);
        actTTF.OnActivate += TagToFilename;
        AddAction(actTTF);
        application.SetAccelsForAction("win.tagToFilename", new string[] { "<Ctrl>T" });
        //Insert Album Art Action
        var actInsertAlbumArt = Gio.SimpleAction.New("insertAlbumArt", null);
        actInsertAlbumArt.OnActivate += InsertAlbumArt;
        AddAction(actInsertAlbumArt);
        application.SetAccelsForAction("win.insertAlbumArt", new string[] { "<Ctrl><Shift>O" });
        //Remove Album Art Action
        var actRemoveAlbumArt = Gio.SimpleAction.New("removeAlbumArt", null);
        actRemoveAlbumArt.OnActivate += RemoveAlbumArt;
        AddAction(actRemoveAlbumArt);
        application.SetAccelsForAction("win.removeAlbumArt", new string[] { "<Ctrl>Delete" });
        //Download MusicBrainz Metadata Action
        var actMusicBrainz = Gio.SimpleAction.New("downloadMusicBrainzMetadata", null);
        actMusicBrainz.OnActivate += DownloadMusicBrainzMetadata;
        AddAction(actMusicBrainz);
        application.SetAccelsForAction("win.downloadMusicBrainzMetadata", new string[] { "<Ctrl>m" });
        //Submit to AcoustId Action
        var actAcoustId = Gio.SimpleAction.New("submitToAcoustId", null);
        actAcoustId.OnActivate += SubmitToAcoustId;
        AddAction(actAcoustId);
        application.SetAccelsForAction("win.submitToAcoustId", new string[] { "<Ctrl>u" });
        //Preferences Action
        var actPreferences = Gio.SimpleAction.New("preferences", null);
        actPreferences.OnActivate += Preferences;
        AddAction(actPreferences);
        application.SetAccelsForAction("win.preferences", new string[] { "<Ctrl>comma" });
        //Keyboard Shortcuts Action
        var actKeyboardShortcuts = Gio.SimpleAction.New("keyboardShortcuts", null);
        actKeyboardShortcuts.OnActivate += KeyboardShortcuts;
        AddAction(actKeyboardShortcuts);
        application.SetAccelsForAction("win.keyboardShortcuts", new string[] { "<Ctrl>question" });
        //Quit Action
        var actQuit = Gio.SimpleAction.New("quit", null);
        actQuit.OnActivate += Quit;
        AddAction(actQuit);
        application.SetAccelsForAction("win.quit", new string[] { "<Ctrl>q" });
        //About Action
        var actAbout = Gio.SimpleAction.New("about", null);
        actAbout.OnActivate += About;
        AddAction(actAbout);
        application.SetAccelsForAction("win.about", new string[] { "F1" });
        //Drop Target
        _dropTarget = Gtk.DropTarget.New(Gio.FileHelper.GetGType(), Gdk.DragAction.Copy);
        _dropTarget.OnDrop += OnDrop;
        AddController(_dropTarget);
    }

    /// <summary>
    /// Constructs a MainWindow
    /// </summary>
    /// <param name="controller">The MainWindowController</param>
    /// <param name="application">The Adw.Application</param>
    public MainWindow(MainWindowController controller, Adw.Application application) : this(Builder.FromFile("window.ui"), controller, application)
    {
    }

    /// <summary>
    /// Starts the MainWindow
    /// </summary>
    public async Task StartAsync()
    {
        _application.AddWindow(this);
        Present();
        SetLoadingState(_("Loading music files from folder..."));
        await _controller.StartupAsync();
    }

    /// <summary>
    /// Occurs when a notification is sent from the controller
    /// </summary>
    /// <param name="sender">object?</param>
    /// <param name="e">NotificationSentEventArgs</param>
    private void NotificationSent(object? sender, NotificationSentEventArgs e)
    {
        var toast = Adw.Toast.New(e.Message);
        _toastOverlay.AddToast(toast);
    }

    /// <summary>
    /// Occurs when a shell notification is sent from the controller
    /// </summary>
    /// <param name="sender">object?</param>
    /// <param name="e">ShellNotificationSentEventArgs</param>
    private void ShellNotificationSent(object? sender, ShellNotificationSentEventArgs? e)
    {
        var notification = Gio.Notification.New(e.Title);
        notification.SetBody(e.Message);
        notification.SetPriority(e.Severity switch
        {
            NotificationSeverity.Success => Gio.NotificationPriority.High,
            NotificationSeverity.Warning => Gio.NotificationPriority.Urgent,
            NotificationSeverity.Error => Gio.NotificationPriority.Urgent,
            _ => Gio.NotificationPriority.Normal
        });
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP")))
        {
            notification.SetIcon(Gio.ThemedIcon.New($"{_controller.AppInfo.ID}-symbolic"));
        }
        else
        {
            var iconHandle = g_file_icon_new(g_file_new_for_path($"{Environment.GetEnvironmentVariable("SNAP")}/usr/share/icons/hicolor/symbolic/apps/{_controller.AppInfo.ID}-symbolic.svg"));
            g_notification_set_icon(notification.Handle, iconHandle);
        }
        _application.SendNotification(_controller.AppInfo.ID, notification);
    }

    /// <summary>
    /// Sets the app into a loading state
    /// </summary>
    /// <param name="message">The message to show on the loading screen</param>
    private void SetLoadingState(string message)
    {
        _viewStack.SetVisibleChildName("Loading");
        _loadingLabel.SetText(message);
        _openFolderButton.SetVisible(false);
        _reloadFolderButton.SetVisible(false);
        _applyButton.SetVisible(false);
        _tagActionsButton.SetVisible(false);
        _webServicesButton.SetVisible(false);
    }

    /// <summary>
    /// Occurs when the window tries to close
    /// </summary>
    /// <param name="sender">Gtk.Window</param>
    /// <param name="e">EventArgs</param>
    /// <returns>True to stop close, else false</returns>
    private bool OnCloseRequested(Gtk.Window sender, EventArgs e)
    {
        if (!_controller.CanClose)
        {
            var dialog = new MessageDialog(this, _controller.AppInfo.ID, _("Apply Changes?"), _("Some music files still have changes waiting to be applied. What would you like to do?"), _("Cancel"), _("Discard"), _("Apply"));
            dialog.OnResponse += async (s, ex) =>
            {
                if(dialog.Response == MessageDialogResponse.Suggested)
                {
                    SetLoadingState(_("Saving tags..."));
                    await _controller.SaveAllTagsAsync(false);
                    Close();
                }
                else if(dialog.Response == MessageDialogResponse.Destructive)
                {
                    _controller.ForceAllowClose();
                    Close();
                }
                dialog.Destroy();
            };
            dialog.Present();
            return true;
        }
        _listMusicFiles.UnselectAll();
        return false;
    }

    /// <summary>
    /// Occurs when something is dropped onto the window
    /// </summary>
    /// <param name="sender">Gtk.DropTarget</param>
    /// <param name="e">Gtk.DropTarget.DropSignalArgs</param>
    private bool OnDrop(Gtk.DropTarget sender, Gtk.DropTarget.DropSignalArgs e)
    {
        var obj = e.Value.GetObject();
        if (obj != null)
        {
            var path = g_file_get_path(obj.Handle);
            if (Directory.Exists(path))
            {
                SetLoadingState(_("Loading music files from folder..."));
                _controller.OpenFolderAsync(path).Wait();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Occurs when the open folder action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void OpenFolder(Gio.SimpleAction sender, EventArgs e)
    {
        var folderDialog = gtk_file_dialog_new();
        gtk_file_dialog_set_title(folderDialog, _("Open Folder"));
        _openCallback = async (source, res, data) =>
        {
            var fileHandle = gtk_file_dialog_select_folder_finish(folderDialog, res, IntPtr.Zero);
            if (fileHandle != IntPtr.Zero)
            {
                var path = g_file_get_path(fileHandle);
                SetLoadingState(_("Loading music files from folder..."));
                await _controller.OpenFolderAsync(path);
            }
        };
        gtk_file_dialog_select_folder(folderDialog, Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
    }

    /// <summary>
    /// Occurs when the reload folder action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private async void ReloadFolder(Gio.SimpleAction sender, EventArgs e)
    {
        if(!_controller.CanClose)
        {
            var dialog = new MessageDialog(this, _controller.AppInfo.ID, _("Apply Changes?"), _("Some music files still have changes waiting to be applied. What would you like to do?"), _("Cancel"), _("Discard"), _("Apply"));
            dialog.OnResponse += async (s, ex) =>
            {
                if(dialog.Response == MessageDialogResponse.Suggested)
                {
                    SetLoadingState(_("Saving tags..."));
                    await _controller.SaveAllTagsAsync(false);
                }
                if(dialog.Response != MessageDialogResponse.Cancel)
                {
                    SetLoadingState(_("Loading music files from folder..."));
                    await _controller.ReloadFolderAsync();
                }
                dialog.Destroy();
            };
            dialog.Present();
        }
        else
        {
            SetLoadingState(_("Loading music files from folder..."));
            await _controller.ReloadFolderAsync();
        }
    }

    /// <summary>
    /// Occurs when the apply action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private async void Apply(Gio.SimpleAction sender, EventArgs e)
    {
        SetLoadingState(_("Saving tags..."));
        await _controller.SaveSelectedTagsAsync();
    }

    /// <summary>
    /// Occurs when the discard unapplied changes action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private async void DiscardUnappliedChanges(Gio.SimpleAction sender, EventArgs e)
    {
        SetLoadingState(_("Discarding unapplied changes..."));
        await _controller.DiscardSelectedUnappliedChangesAsync();
    }

    /// <summary>
    /// Occurs when the delete tags action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void DeleteTags(Gio.SimpleAction sender, EventArgs e) => _controller.DeleteSelectedTags();

    /// <summary>
    /// Occurs when the filename to tag action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void FilenameToTag(Gio.SimpleAction sender, EventArgs e)
    {
        var dialog = new ComboBoxDialog(this, _controller.AppInfo.ID, _("File Name to Tag"), _("Please select a format string."), _("Format String"),
            new string[] { "%artist%- %title%", "%title%- %artist%", "%track%- %title%", "%title%" }, _("Cancel"), _("Convert"));
        dialog.OnResponse += (s, ex) =>
        {
            if(!string.IsNullOrEmpty(dialog.Response))
            {
                _controller.FilenameToTag(dialog.Response);
            }
            dialog.Destroy();
        };
        dialog.Present();
    }

    /// <summary>
    /// Occurs when the tag to filename action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void TagToFilename(Gio.SimpleAction sender, EventArgs e)
    {
        var dialog = new ComboBoxDialog(this, _controller.AppInfo.ID, _("Tag to File Name"), _("Please select a format string."), _("Format String"),
            new string[] { "%artist%- %title%", "%title%- %artist%", "%track%- %title%", "%title%" }, _("Cancel"), _("Convert"));
        dialog.OnResponse += (s, ex) =>
        {
            if(!string.IsNullOrEmpty(dialog.Response))
            {
                _controller.TagToFilename(dialog.Response);
            }
            dialog.Destroy();
        };
        dialog.Present();
    }

    /// <summary>
    /// Occurs when the insert album art action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void InsertAlbumArt(Gio.SimpleAction sender, EventArgs e)
    {
        var filterImages = Gtk.FileFilter.New();
        filterImages.AddMimeType("image/*");
        var openFileDialog = gtk_file_dialog_new();
        gtk_file_dialog_set_title(openFileDialog, _("Insert Album Art"));
        var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
        filters.Append(filterImages);
        gtk_file_dialog_set_filters(openFileDialog, filters.Handle);
        _openCallback = async (source, res, data) =>
        {
            var fileHandle = gtk_file_dialog_open_finish(openFileDialog, res, IntPtr.Zero);
            if (fileHandle != IntPtr.Zero)
            {
                var path = g_file_get_path(fileHandle);
                SetLoadingState(_("Inserting album art..."));
                await _controller.InsertSelectedAlbumArtAsync(path);
            }
        };
        gtk_file_dialog_open(openFileDialog, Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
    }

    /// <summary>
    /// Occurs when the remove album art action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void RemoveAlbumArt(Gio.SimpleAction sender, EventArgs e) => _controller.RemoveSelectedAlbumArt();

    /// <summary>
    /// Occurs when the download musicbrainz metadata action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private async void DownloadMusicBrainzMetadata(Gio.SimpleAction sender, EventArgs e)
    {
        SetLoadingState(_("Downloading MusicBrainz metadata..."));
        await _controller.DownloadMusicBrainzMetadataAsync();
    }

    /// <summary>
    /// Occurs when the submit to acoustid action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void SubmitToAcoustId(Gio.SimpleAction sender, EventArgs e)
    {
        if(_controller.SelectedMusicFiles.Count > 1)
        {
            var dialog = new MessageDialog(this, _controller.AppInfo.ID, _("Too Many Files Selected"), _("Only one file can be submitted to AcoustID at a time. Please select only one file and try again."), _("OK"));
            dialog.OnResponse += (s, ex) => dialog.Destroy();
            dialog.Present();
            return;
        }
        var entryDialog = new EntryDialog(this, _controller.AppInfo.ID, _("Submit to AcoustId"), _("AcoustId can associate a song's fingerprint with a MusicBrainz Recording Id for easy identification.\n\nIf you have a MusicBrainz Recording Id for this song, please provide it below.\n\nIf none is provided, Tagger will submit your tag's metadata in association with the fingerprint instead."), _("MusicBrainz Recording Id"), _("Cancel"), _("Submit"));
        entryDialog.OnResponse += async (s, ex) =>
        {
            if(!string.IsNullOrEmpty(entryDialog.Response))
            {
                if(!_controller.CanClose)
                {
                    var dialog = new MessageDialog(this, _controller.AppInfo.ID, _("Apply Changes?"), _("Some music files still have changes waiting to be applied. What would you like to do?"), _("Cancel"), _("Discard"), _("Apply"));
                    dialog.OnResponse += async (ss, exx) =>
                    {
                        if(dialog.Response == MessageDialogResponse.Suggested)
                        {
                            SetLoadingState(_("Saving tags..."));
                            await _controller.SaveAllTagsAsync(false);
                        }
                        if(dialog.Response != MessageDialogResponse.Cancel)
                        {
                            SetLoadingState(_("Submitting data to AcoustId..."));
                            await _controller.SubmitToAcoustIdAsync(entryDialog.Response == "NULL" ? null : entryDialog.Response);
                        }
                        dialog.Destroy();
                    };
                    dialog.Present();
                }
                else
                {
                    SetLoadingState(_("Submitting data to AcoustId..."));
                    await _controller.SubmitToAcoustIdAsync(entryDialog.Response == "NULL" ? null : entryDialog.Response);
                }
            }
            entryDialog.Destroy();
        };
        entryDialog.Present();
    }

    /// <summary>
    /// Occurs when the preferences action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void Preferences(Gio.SimpleAction sender, EventArgs e)
    {
        var preferencesDialog = new PreferencesDialog(_controller.CreatePreferencesViewController(), _application, this);
        if(!_controller.CanClose)
        {
            var dialog = new MessageDialog(this, _controller.AppInfo.ID, _("Apply Changes?"), _("Some music files still have changes waiting to be applied. What would you like to do?"), _("Cancel"), _("Discard"), _("Apply"));
            dialog.OnResponse += async (ss, exx) =>
            {
                if(dialog.Response != MessageDialogResponse.Cancel)
                {
                    preferencesDialog.Present();
                }
                if(dialog.Response == MessageDialogResponse.Suggested)
                {
                    SetLoadingState(_("Saving tags..."));
                    await _controller.SaveAllTagsAsync(true);
                }
                dialog.Destroy();
            };
            dialog.Present();
        }
        else
        {
            preferencesDialog.Present();
        }
    }

    /// <summary>
    /// Occurs when the keyboard shortcuts action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void KeyboardShortcuts(Gio.SimpleAction sender, EventArgs e)
    {
        var builder = Builder.FromFile("shortcuts_dialog.ui");
        var shortcutsWindow = (Gtk.ShortcutsWindow)builder.GetObject("_shortcuts");
        shortcutsWindow.SetTransientFor(this);
        shortcutsWindow.SetIconName(_controller.AppInfo.ID);
        shortcutsWindow.Present();
    }

    /// <summary>
    /// Occurs when quit action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void Quit(Gio.SimpleAction sender, EventArgs e) => _application.Quit();

    /// <summary>
    /// Occurs when the about action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void About(Gio.SimpleAction sender, EventArgs e)
    {
        var debugInfo = new StringBuilder();
        debugInfo.AppendLine(_controller.AppInfo.ID);
        debugInfo.AppendLine(_controller.AppInfo.Version);
        debugInfo.AppendLine($"GTK {Gtk.Functions.GetMajorVersion()}.{Gtk.Functions.GetMinorVersion()}.{Gtk.Functions.GetMicroVersion()}");
        debugInfo.AppendLine($"libadwaita {Adw.Functions.GetMajorVersion()}.{Adw.Functions.GetMinorVersion()}.{Adw.Functions.GetMicroVersion()}");
        if (File.Exists("/.flatpak-info"))
        {
            debugInfo.AppendLine("Flatpak");
        }
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP")))
        {
            debugInfo.AppendLine("Snap");
        }
        debugInfo.AppendLine(CultureInfo.CurrentCulture.ToString());
        var localeProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "locale",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };
        try
        {
            localeProcess.Start();
            var localeString = localeProcess.StandardOutput.ReadToEnd().Trim();
            localeProcess.WaitForExit();
            debugInfo.AppendLine(localeString);
        }
        catch
        {
            debugInfo.AppendLine("Unknown locale");
        }
        var dialog = Adw.AboutWindow.New();
        dialog.SetTransientFor(this);
        dialog.SetIconName(_controller.AppInfo.ID);
        dialog.SetApplicationName(_controller.AppInfo.ShortName);
        dialog.SetApplicationIcon(_controller.AppInfo.ID + (_controller.AppInfo.GetIsDevelVersion() ? "-devel" : ""));
        dialog.SetVersion(_controller.AppInfo.Version);
        dialog.SetDebugInfo(debugInfo.ToString());
        dialog.SetComments(_controller.AppInfo.Description);
        dialog.SetDeveloperName("Nickvision");
        dialog.SetLicenseType(Gtk.License.MitX11);
        dialog.SetCopyright("© Nickvision 2021-2023");
        dialog.SetWebsite("https://nickvision.org/");
        dialog.SetIssueUrl(_controller.AppInfo.IssueTracker.ToString());
        dialog.SetSupportUrl(_controller.AppInfo.SupportUrl.ToString());
        dialog.AddLink(_("GitHub Repo"), _controller.AppInfo.GitHubRepo.ToString());
        dialog.AddLink(_("Matrix Chat"), "https://matrix.to/#/#nickvision:matrix.org");
        dialog.SetDevelopers(_("Nicholas Logozzo {0}\nContributors on GitHub ❤️ {1}", "https://github.com/nlogozzo", "https://github.com/NickvisionApps/Denaro/graphs/contributors").Split("\n"));
        dialog.SetDesigners(_("Nicholas Logozzo {0}\nFyodor Sobolev {1}\nDaPigGuy {2}", "https://github.com/nlogozzo", "https://github.com/fsobolev", "https://github.com/DaPigGuy").Split("\n"));
        dialog.SetArtists(_("David Lapshin {0}", "https://github.com/daudix-UFO").Split("\n"));
        dialog.SetTranslatorCredits(_("translator-credits"));
        dialog.SetReleaseNotes(_controller.AppInfo.Changelog);
        dialog.Present();
    }

    /// <summary>
    /// Occurs when the music folder is updated
    /// </summary>
    /// <param name="data">bool on whether or not to send a toast of loaded files</param>
    private bool MusicFolderUpdated(nint data)
    {
        var handle = GCHandle.FromIntPtr(data);
        var target = (bool?)handle.Target;
        if(target != null)
        {
            var sendToast = target.Value;
            _listMusicFiles.UnselectAll();
            foreach(var row in _listMusicFilesRows)
            {
                _listMusicFiles.Remove(row);
            }
            _listMusicFilesRows.Clear();
            if(!string.IsNullOrEmpty(_controller.MusicFolderPath))
            {
                foreach(var musicFile in _controller.MusicFiles)
                {
                    var row = Adw.ActionRow.New();
                    if(!string.IsNullOrEmpty(musicFile.Title))
                    {
                        row.SetTitle($"{(musicFile.Track != 0 ? $"{musicFile.Track:D2} - " : "")}{Regex.Replace(musicFile.Title, "\\&", "&amp;")}");
                        row.SetSubtitle(Regex.Replace(musicFile.Filename, "\\&", "&amp;"));
                    }
                    else
                    {
                        row.SetTitle(Regex.Replace(musicFile.Filename, "\\&", "&amp;"));
                        row.SetSubtitle("");
                    }
                    _listMusicFiles.Append(row);
                    _listMusicFilesRows.Add(row);
                }
                _headerBar.RemoveCssClass("flat");
                _title.SetSubtitle(_controller.MusicFolderPath);
                _openFolderButton.SetVisible(true);
                _reloadFolderButton.SetVisible(true);
                _applyButton.SetVisible(false);
                _tagActionsButton.SetVisible(false);
                _webServicesButton.SetVisible(false);
                _viewStack.SetVisibleChildName("Folder");
                _filesViewStack.SetVisibleChildName(_controller.MusicFiles.Count > 0 ? "Files" : "NoFiles");
                if(sendToast)
                {
                    _toastOverlay.AddToast(Adw.Toast.New(string.Format(_("Loaded {0} music files."), _controller.MusicFiles.Count)));
                }
            }
            else
            {
                _headerBar.AddCssClass("flat");
                _title.SetSubtitle("");
                _openFolderButton.SetVisible(false);
                _reloadFolderButton.SetVisible(false);
                _applyButton.SetVisible(false);
                _tagActionsButton.SetVisible(false);
                _webServicesButton.SetVisible(false);
                _viewStack.SetVisibleChildName("NoFolder");
            }
        }
        handle.Free();
        return false;
    }

    /// <summary>
    /// Occurs when a music file's save state is changed
    /// </summary>
    private bool MusicFileSaveStatesChanged()
    {
        _viewStack.SetVisibleChildName("Folder");
        _openFolderButton.SetVisible(true);
        _reloadFolderButton.SetVisible(true);
        _applyButton.SetVisible(_controller.SelectedMusicFiles.Count != 0);
        _tagActionsButton.SetVisible(_controller.SelectedMusicFiles.Count != 0);
        _webServicesButton.SetVisible(_controller.SelectedMusicFiles.Count != 0);
        var i = 0;
        foreach(var saved in _controller.MusicFileSaveStates)
        {
            _listMusicFilesRows[i].SetIconName(!saved ? "document-modified-symbolic" : "");
            i++;
        }
        return false;
    }

    /// <summary>
    /// Occurs when the selected music files' properties are changed
    /// </summary>
    private bool SelectedMusicFilesPropertiesChanged()
    {
        _isSelectionOccuring = true;
        //Update Properties
        _applyButton.SetVisible(_controller.SelectedMusicFiles.Count != 0);
        _tagActionsButton.SetVisible(_controller.SelectedMusicFiles.Count != 0);
        _webServicesButton.SetVisible(_controller.SelectedMusicFiles.Count != 0);
        _filenameRow.SetEditable(_controller.SelectedMusicFiles.Count < 2);
        if(_controller.SelectedMusicFiles.Count == 0)
        {
            _musicFilesSearch.SetText("");
        }
        _filenameRow.SetText(_controller.SelectedPropertyMap.Filename);
        _titleRow.SetText(_controller.SelectedPropertyMap.Title);
        _artistRow.SetText(_controller.SelectedPropertyMap.Artist);
        _albumRow.SetText(_controller.SelectedPropertyMap.Album);
        _yearRow.SetText(_controller.SelectedPropertyMap.Year);
        _trackRow.SetText(_controller.SelectedPropertyMap.Track);
        _albumArtistRow.SetText(_controller.SelectedPropertyMap.AlbumArtist);
        _genreRow.SetText(_controller.SelectedPropertyMap.Genre);
        _commentRow.SetText(_controller.SelectedPropertyMap.Comment);
        _durationRow.SetText(_controller.SelectedPropertyMap.Duration);
        _fingerprintRow.SetText(_controller.SelectedPropertyMap.Fingerprint);
        _fileSizeRow.SetText(_controller.SelectedPropertyMap.FileSize);
        if(_controller.SelectedPropertyMap.AlbumArt == "hasArt")
        {
            _artViewStack.SetVisibleChildName("Image");
            var art = _controller.SelectedMusicFiles.First().Value.AlbumArt;
            if(art.IsEmpty)
            {
                _albumArtImage.Clear();
            }
            else
            {
                var loader = gdk_pixbuf_loader_new();
                gdk_pixbuf_loader_write(loader, art.Data, (uint)art.Data.Length, 0);
                gtk_image_set_from_pixbuf(_albumArtImage.Handle, gdk_pixbuf_loader_get_pixbuf(loader));
                gdk_pixbuf_loader_close(loader, 0);
                g_object_unref(loader);
            }
        }
        else if(_controller.SelectedPropertyMap.AlbumArt == "keepArt")
        {
            _artViewStack.SetVisibleChildName("KeepImage");
            _albumArtImage.Clear();
        }
        else
        {
            _artViewStack.SetVisibleChildName("NoImage");
            _albumArtImage.Clear();
        }
        //Update Rows
        foreach(var pair in _controller.SelectedMusicFiles)
        {
            if(!string.IsNullOrEmpty(pair.Value.Title))
            {
                _listMusicFilesRows[pair.Key].SetTitle($"{(pair.Value.Track != 0 ? $"{pair.Value.Track:D2} - " : "")}{Regex.Replace(pair.Value.Title, "\\&", "&amp;")}");
                _listMusicFilesRows[pair.Key].SetSubtitle(Regex.Replace(pair.Value.Filename, "\\&", "&amp;"));
            }
            else
            {
                _listMusicFilesRows[pair.Key].SetTitle(Regex.Replace(pair.Value.Filename, "\\&", "&amp;"));
                _listMusicFilesRows[pair.Key].SetSubtitle("");
            }
        }
        _isSelectionOccuring = false;
        return false;
    }

    /// <summary>
    /// Occurs when the _musicFilesSearch's text is changed
    /// </summary>
    /// <param name="sender">Gtk.SearchEntry</param>
    /// <param name="e">EventArgs</param>
    private void SearchChanged(Gtk.SearchEntry sender, EventArgs e)
    {
        var search = _musicFilesSearch.GetText().ToLower();
        if(!string.IsNullOrEmpty(search) && search[0] == '!')
        {
            _advancedSearchInfoButton.SetVisible(true);
            var result = _controller.AdvancedSearch(search);
            if(!result.Success)
            {
                _musicFilesSearch.RemoveCssClass("success");
                _musicFilesSearch.AddCssClass("error");
                _listMusicFiles.SetFilterFunc((row) => true);
            }
            else
            {
                _musicFilesSearch.RemoveCssClass("error");
                _musicFilesSearch.AddCssClass("success");
                _listMusicFiles.SetFilterFunc((row) =>
                {
                    if(result.LowerFilenames!.Count == 0)
                    {
                        return false;
                    }
                    var rowFilename = (row as Adw.ActionRow)!.GetSubtitle() ?? "";
                    if(string.IsNullOrEmpty(rowFilename))
                    {
                        rowFilename = (row as Adw.ActionRow)!.GetTitle();
                    }
                    rowFilename = rowFilename.ToLower();
                    return result.LowerFilenames.Contains(rowFilename);
                });
            }
        }
        else
        {
            _advancedSearchInfoButton.SetVisible(false);
            _musicFilesSearch.RemoveCssClass("success");
            _musicFilesSearch.RemoveCssClass("error");
            _listMusicFiles.SetFilterFunc((row) =>
            {
                var rowFilename = (row as Adw.ActionRow)!.GetSubtitle() ?? "";
                if(string.IsNullOrEmpty(rowFilename))
                {
                    rowFilename = (row as Adw.ActionRow)!.GetTitle();
                }
                rowFilename = rowFilename.ToLower();
                if(string.IsNullOrEmpty(search) || rowFilename.Contains(search))
                {
                    return true;
                }
                return false;
            });
        }
    }

    /// <summary>
    /// Occurs when the _advancedSearchInfoButton is clicked
    /// </summary>
    /// <param name="sender">Gtk.Button</param>
    /// <param name="e">EventArgs</param>
    private void AdvancedSearchInfo(Gtk.Button sender, EventArgs e)
    {
        var dialog = new MessageDialog(this, _controller.AppInfo.ID, _("Advanced Search"), _controller.AdvancedSearchInfo, _("OK"));
        dialog.SetSizeRequest(700, -1);
        dialog.OnResponse += (s, ex) => dialog.Destroy();
        dialog.Present();
    }

    /// <summary>
    /// Occurs when the _listMusicFiles's selection is changed
    /// </summary>
    /// <param name="sender">Gtk.ListBox</param>
    /// <param name="e">EventArgs</param>
    private unsafe void ListMusicFiles_SelectionChanged(Gtk.ListBox sender, EventArgs e)
    {
        _isSelectionOccuring = true;
        var selectedIndexes = new List<int>();
        var firstSelectedRowPtr = gtk_list_box_get_selected_rows(_listMusicFiles.Handle);
        for(var ptr = firstSelectedRowPtr; ptr != null; ptr = ptr->next)
        {
            selectedIndexes.Add(gtk_list_box_row_get_index(ptr->data));
        }
        _controller.UpdateSelectedMusicFiles(selectedIndexes);
        _isSelectionOccuring = false;
    }

    /// <summary>
    /// Occurs when a tag property's row text is changed
    /// </summary>
    private void TagPropertyChanged()
    {
        if(!_isSelectionOccuring && _controller.SelectedMusicFiles.Count > 0)
        {
            //Update Tags
            _controller.UpdateTags(new PropertyMap()
            {
                Filename = _filenameRow.GetText(),
                Title = _titleRow.GetText(),
                Artist = _artistRow.GetText(),
                Album = _albumRow.GetText(),
                Year = _yearRow.GetText(),
                Track = _trackRow.GetText(),
                AlbumArtist = _albumArtistRow.GetText(),
                Genre = _genreRow.GetText(),
                Comment = _commentRow.GetText()
            }, false);
            //Update Rows
            foreach(var pair in _controller.SelectedMusicFiles)
            {
                if(!string.IsNullOrEmpty(pair.Value.Title))
                {
                    _listMusicFilesRows[pair.Key].SetTitle($"{(pair.Value.Track != 0 ? $"{pair.Value.Track:D2} - " : "")}{Regex.Replace(pair.Value.Title, "\\&", "&amp;")}");
                    _listMusicFilesRows[pair.Key].SetSubtitle(Regex.Replace(pair.Value.Filename, "\\&", "&amp;"));
                }
                else
                {
                    _listMusicFilesRows[pair.Key].SetTitle(Regex.Replace(pair.Value.Filename, "\\&", "&amp;"));
                    _listMusicFilesRows[pair.Key].SetSubtitle("");
                }
            }
        }
    }
}

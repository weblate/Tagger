#pragma once

#include <functional>
#include <string>
#include <vector>
#include "preferencesdialogcontroller.hpp"
#include "../models/appinfo.hpp"
#include "../models/configuration.hpp"
#include "../models/musicfile.hpp"
#include "../models/musicfolder.hpp"

namespace NickvisionTagger::Controllers
{
    /**
     * A controller for the MainWindow
     */
    class MainWindowController
    {
    public:
    	/**
    	 * Constructs a MainWindowController
    	 *
    	 * @param appInfo The AppInfo for the application (Stored as a reference)
    	 * @param configuration The Configuration for the application (Stored as a reference)
    	 */
    	MainWindowController(NickvisionTagger::Models::AppInfo& appInfo, NickvisionTagger::Models::Configuration& configuration);
    	/**
    	 * Gets the AppInfo object representing the application's information
    	 *
    	 * @returns The AppInfo object for the application
    	 */
    	const NickvisionTagger::Models::AppInfo& getAppInfo() const;
    	/**
    	 * Creates a PreferencesDialogController
    	 *
    	 * @returns A new PreferencesDialogController
    	 */
    	PreferencesDialogController createPreferencesDialogController() const;
    	/**
    	 * Registers a callback for sending a toast notification on the MainWindow
    	 *
    	 * @param callback A void(const std::string&) function
    	 */
    	void registerSendToastCallback(const std::function<void(const std::string& message)>& callback);
    	 /**
    	 * Registers a callback for sending a desktop notification
    	 *
    	 * @param callback A void(const std::string&, const std::string&) function
    	 */
    	void registerSendNotificationCallback(const std::function<void(const std::string& title, const std::string& message)>& callback);
    	/**
    	 * Runs startup functions
    	 */
    	void startup();
    	/**
    	 * Updates the controller based on the configuration changes
    	 */
    	void onConfigurationChanged();
    	/**
    	 * Gets the opened music folder path
    	 *
    	 * @returns The opened music folder path or "No Folder Path" if no folder is opened
    	 */
 	const std::filesystem::path& getMusicFolderPath() const;
 	/**
 	 * Gets the number of music files in the music folder
 	 *
 	 * @returns The number of music files in the music folder (0 if no folder is opened)
 	 */
 	std::size_t getMusicFileCount() const;
 	/**
 	 * Gets the list of music files in the music folder
 	 *
 	 * @returns The list of music files in the music folder
 	 */
	const std::vector<std::shared_ptr<NickvisionTagger::Models::MusicFile>>& getMusicFiles() const;
    	/**
    	 * Opens a music folder with the given path
    	 * 
    	 * @param folderPath The path to the folder to open
    	 */
    	void openMusicFolder(const std::string& folderPath);
    	/**
    	 * Reloads a music folder
    	 */
    	void reloadMusicFolder();
    	/**
    	 * Registers a callback for when the music folder is changed
    	 *
    	 * @param callback A void() function
    	 */
    	void registerMusicFolderUpdatedCallback(const std::function<void()>& callback);
    	/**
    	 * Gets the list of selected music files from the UI
    	 *
    	 * @returns The list of selected music files from the UI
    	 */
    	const std::vector<std::shared_ptr<NickvisionTagger::Models::MusicFile>>& getSelectedMusicFiles() const;
    	/**
    	 * updates the list of selected music files from the list of indexes
    	 *
    	 * @param indexes The list of selected indexes
    	 */
    	void updateSelectedMusicFiles(std::vector<int> indexes);
    	
    private:
    	NickvisionTagger::Models::AppInfo& m_appInfo;
    	NickvisionTagger::Models::Configuration& m_configuration;
    	bool m_isOpened;
    	std::function<void(const std::string& message)> m_sendToastCallback;
    	std::function<void(const std::string& title, const std::string& message)> m_sendNotificationCallback;
    	NickvisionTagger::Models::MusicFolder m_musicFolder;
    	std::function<void()> m_musicFolderUpdatedCallback;
    	std::vector<std::shared_ptr<NickvisionTagger::Models::MusicFile>> m_selectedMusicFiles;
    };
}
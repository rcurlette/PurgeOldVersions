# PurgeOldVersions
Purge old versions of Tridion items using the Core Service

# Usage:
- In the Deployment folder, copy the PurgeOldVersions.exe and PurgeOldVersions.exe.config files to your local system or the server.
- Open the PurgeOldVersions.exe.config Config file
- Update the Username and Password variables
- Add the Publications, Folders and Structure Group URIs, separated by a comma
- If running from a workstation and _not_ the server, update the Core Service binding paramters.  Change http://localhost to the URL of your CMS server.
- Run the .exe from a command prompt in interactive mode (=true) or schedule it with Windows Scheduler and set interactive mode to false

# PurgeOldVersions
Purge old versions of Tridion items using the Core Service

# Usage:
- In the Deployment folder, copy the PurgeOldVersions.exe and PurgeOldVersions.exe.config files to your local system or the server.
- Open the PurgeOldVersions.exe.config Config file
- Update the Username and Password variables

# Config Variables:
Please set the following variables in the PurgeOldVersions.exe.config file.  

- *allPublications* - set to true to run for entire CMS, false to run on specific Publications
- *publications* - used to run on specific Publications.  allPublications must be false.
- *contentFolders* - the root folder(s) to scan and run tool on.  
- *structureGroups* - the structure groups to parse and remove old versions of pages.  
- *runInInteractiveMode* - displays the output in a console window

_If the variables for *contentFolders* or *structureGroups* is empty, it will not parse them.  Also, the folder in the right Publication will be done by the script, so it's usually the folder in the parent level.

# Interactive Mode or Scheduled:
- If running from a workstation and _not_ the server, update the Core Service binding paramters.  Change http://localhost to the URL of your CMS server.
- Run the .exe from a command prompt in interactive mode (=true) or schedule it with Windows Scheduler and set interactive mode to false

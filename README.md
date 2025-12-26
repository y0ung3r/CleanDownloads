CleanDownloads
==========
**CleanDownloads** is a background utility that automatically deletes a file from the Downloads folder immediately after it is used for the first time

![Demonstration](https://github.com/user-attachments/assets/b585a59a-01d9-49ca-8b47-ff01d57a9f4e)

### Installation
1. Download the exe file from the [releases](https://github.com/y0ung3r/CleanDownloads/releases) page ([or compile it from the source code](#build-from-source)).
2. Run the exe file
   - It installs itself in the `%LocalAppData%\CleanDownloads` folder
   - Creates a shortcut in the Start Menu: `Clean Downloads`
   - Registers an autostart entry for the current user via the `Windows Task Scheduler`
3. In the window that opens, you can select the file deletion mode:

   ![Configuration window](https://github.com/user-attachments/assets/09285fd2-c72a-4fbf-843c-f8847f052eea)

### Notes
- If you launch the application again while it is already running, the existing instance window (single instance) will open
- No icon in the tray: launch the window again by running the shortcut or exe file

### Uninstall / Disable autostart
To disable autostart only:
  - Delete the `Clean Downloads` scheduled task (`Task Scheduler` > `Task Scheduler Library`).
  - Or from the terminal (`PowerShell`/`CMD`):
    ```
    schtasks /Delete /TN "Clean Downloads" /F
    ```

To completely uninstall:
1. Close the application (kill the `Clean Downloads` process in `Task Manager` or log out of the system)
2. Delete the `%LocalAppData%\CleanDownloads` folder
3. Remove the shortcut in the Start menu, if it exists: `%ProgramData%\Microsoft\Windows\Start Menu\Programs\Clean Downloads.lnk`
4. Remove the scheduled task (see above), if it still exists

### Build from source
Prerequisites:

- `.NET SDK 10.0`
- `Windows 10/11`

Build and publish (single‑file and self‑contained):

```cmd
dotnet publish
```

The published single‑file exe will be located in the `Clean Downloads\bin\Release\net10.0\win-x64\publish` folder

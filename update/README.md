# Update manifest of HyperPDF

The automatic update feature downloads and installs new versions of HyperPDF using
the manifest at `update/update.json`. The application fetches this file from
`BrandInfo.UpdateCheckUrl` during startup (at most once every 24 hours) and from the
`Check for updates` button in the About dialog.

## Developer workflow to publish an update

1. Bump `ApplicationVersion` in `Directory.Build.props` **and** `BrandInfo.ApplicationVersion`.
2. Build the installer (`tools/build-app.ps1` + `installer/build-installer.ps1`).
3. Upload `HyperPDF-Setup-<version>.exe` to a public location (for example the GitHub
   release of the matching tag).
4. Update `update/update.json`:

   ```json
   {
     "version": "1.1.0",
     "downloadUrl": "https://github.com/HYPERMX7/HyperPDF/releases/download/v1.1.0/HyperPDF-Setup-1.1.0.exe",
     "releaseNotes": "New features and fixes.",
     "sha256": "<sha256 hash of the installer, optional but recommended>"
   }
   ```

5. Push the manifest. Users receive the update popup on their next start.

## Behaviour on the user side

* Startup: a silent check runs once after the main window renders; a popup only appears
  when a newer version exists and has not been skipped.
* `Update now`: downloads the installer to `%TEMP%\HyperPDF\update`, verifies the optional
  SHA256 hash, launches it with `/SILENT` and closes the running HyperPDF instance.
* `Skip this version`: the popup is suppressed for that version during silent checks.
* A failed or offline check is silently ignored; the app keeps working normally.

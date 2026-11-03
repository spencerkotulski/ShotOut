Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!define APP_VERSION "1.4.3"
!ifdef TEST_MODE
!define UNINSTALL_KEY "Software\ShotOutInstallerTests\${TEST_ID}\Uninstall"
!define RUN_KEY "Software\ShotOutInstallerTests\${TEST_ID}\Run"
!define START_LINK "$INSTDIR\TestStartMenu.lnk"
!define DESKTOP_LINK "$INSTDIR\TestDesktop.lnk"
!else
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\ShotOut"
!define RUN_KEY "Software\Microsoft\Windows\CurrentVersion\Run"
!define START_LINK "$SMPROGRAMS\ShotOut.lnk"
!define DESKTOP_LINK "$DESKTOP\ShotOut.lnk"
!endif
Name "ShotOut ${APP_VERSION}"
!ifdef TEST_MODE
OutFile "${TEST_OUT}"
!else
OutFile "ShotOut-Setup-v${APP_VERSION}.exe"
!endif
InstallDir "$LOCALAPPDATA\Programs\ShotOut"
RequestExecutionLevel user
ManifestDPIAware true
SetFont "Segoe UI" 9
SetCompressor /SOLID lzma
SetOverwrite on
VIProductVersion "1.4.3.0"
VIAddVersionKey /LANG=1033 "ProductName" "ShotOut"
VIAddVersionKey /LANG=1033 "ProductVersion" "${APP_VERSION}.0"
VIAddVersionKey /LANG=1033 "FileVersion" "${APP_VERSION}.0"
VIAddVersionKey /LANG=1033 "FileDescription" "ShotOut NSIS installer"
VIAddVersionKey /LANG=1033 "LegalCopyright" "ShotOut"
Icon "ShotOut.ico"
UninstallIcon "ShotOut.ico"
!define MUI_ICON "ShotOut.ico"
!define MUI_UNICON "ShotOut.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADERIMAGE_BITMAP "InstallerAssets\Header.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "InstallerAssets\Sidebar.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "InstallerAssets\Sidebar.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP_STRETCH FitControl
!define MUI_UNWELCOMEFINISHPAGE_BITMAP_STRETCH FitControl
!define MUI_WELCOMEPAGE_TITLE "Welcome to ShotOut"
!define MUI_WELCOMEPAGE_TEXT "Capture, annotate, and share screenshots.$\r$\n$\r$\nThis setup installs ShotOut for your Windows account, with no administrator access required.$\r$\n$\r$\nYour existing settings, screenshots, and projects are preserved.$\r$\n$\r$\nClose ShotOut from its system tray menu before updating."
!define MUI_FINISHPAGE_TITLE "ShotOut is ready"
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\ShotOut.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Open ShotOut"
!define MUI_FINISHPAGE_TEXT "You can now capture, annotate, and share.$\r$\n$\r$\nOpen Settings to customize capture tasks, sounds, shortcuts, and display size.$\r$\n$\r$\nPress F1 in ShotOut for the complete shortcut guide."
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

!macro CheckNotRunning PREFIX
Function ${PREFIX}CheckNotRunning
  IfFileExists "$INSTDIR\ShotOut.exe" 0 stopped
  retry:
  System::Call 'kernel32::CreateFileW(w "$INSTDIR\ShotOut.exe", i 0x40000000, i 7, p 0, i 3, i 0, p 0)p.r0'
  IntCmp $0 -1 locked
  System::Call 'kernel32::CloseHandle(p r0)'
  Goto stopped
  locked:
  IfSilent silent_fail
  MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "ShotOut is running or its files are in use. Exit ShotOut from its tray menu, then choose Retry. Unsaved captures will not be closed automatically." IDRETRY retry
  Abort
  silent_fail:
  SetErrorLevel 2
  Quit
  stopped:
FunctionEnd
!macroend
!insertmacro CheckNotRunning ""
!insertmacro CheckNotRunning "un."

Function CheckInstallDirectory
  IfFileExists "$INSTDIR\.shotout-nsis" nsis_marker
  IfFileExists "$INSTDIR\.shotout-install" legacy_marker
  IfFileExists "$INSTDIR\*.*" 0 accepted
  FindFirst $0 $1 "$INSTDIR\*.*"
  scan:
  StrCmp $1 "" empty
  StrCmp $1 "." next
  StrCmp $1 ".." next
  FindClose $0
  Goto refused
  next:
  FindNext $0 $1
  Goto scan
  empty:
  FindClose $0
  Goto accepted
  nsis_marker:
  FileOpen $0 "$INSTDIR\.shotout-nsis" r
  FileRead $0 $1
  FileClose $0
  StrCmp $1 "ShotOut.NSIS.1" accepted refused
  legacy_marker:
  FileOpen $0 "$INSTDIR\.shotout-install" r
  FileRead $0 $1
  FileClose $0
  StrCmp $1 "ShotOut.ManagedInstall.1" accepted refused
  refused:
  IfSilent refuse_silent
  MessageBox MB_OK|MB_ICONSTOP "The installation folder contains files from an unrecognized installation. No files were changed. Move those files before installing ShotOut here."
  refuse_silent:
  SetErrorLevel 3
  Abort
  accepted:
FunctionEnd

Function .onInit
  SetShellVarContext current
  Call CheckInstallDirectory
  Call CheckNotRunning
FunctionEnd

Section "ShotOut application and documentation" SEC_CORE
  SectionIn RO
  SetShellVarContext current
  Call CheckInstallDirectory
  Call CheckNotRunning
  SetOutPath "$INSTDIR"
  File "ShotOut.exe"
  File "README.md"
  File "Shortcuts.md"
  File "Shortcuts.html"
  File "CHANGELOG.md"
  File "RELEASE-NOTES.md"
  File "SOUND-CREDITS.md"
  File /oname=ShotOut-Brand-1.4.3.ico "ShotOut.ico"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  FileOpen $0 "$INSTDIR\.shotout-nsis" w
  FileWrite $0 "ShotOut.NSIS.1"
  FileClose $0
  Delete "$INSTDIR\.shotout-install"
  CreateShortCut "${START_LINK}" "$INSTDIR\ShotOut.exe" "" "$INSTDIR\ShotOut-Brand-1.4.3.ico"
  ReadRegStr $0 HKCU "${RUN_KEY}" "ShotOut"
  ReadRegStr $1 HKCU "${RUN_KEY}" "SnapCanvas"
  ${If} $0 != ""
  ${OrIf} $1 != ""
    WriteRegStr HKCU "${RUN_KEY}" "ShotOut" '$\"$INSTDIR\ShotOut.exe$\" --startup'
    DeleteRegValue HKCU "${RUN_KEY}" "SnapCanvas"
  ${EndIf}
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName" "ShotOut"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher" "ShotOut"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\ShotOut-Brand-1.4.3.ico"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKCU "${UNINSTALL_KEY}" "QuietUninstallString" '$\"$INSTDIR\Uninstall.exe$\" /S'
  WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallerBackend" "NSIS"
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair" 1
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "EstimatedSize" 1600
  System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0, p 0, p 0)'
SectionEnd
Section "Desktop shortcut" SEC_DESKTOP
  CreateShortCut "${DESKTOP_LINK}" "$INSTDIR\ShotOut.exe" "" "$INSTDIR\ShotOut-Brand-1.4.3.ico"
SectionEnd
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_CORE} "Installs the app, three bundled sounds, shortcut guide and Start menu shortcut."
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_DESKTOP} "Adds or updates a shortcut on your desktop."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Function un.onInit
  SetShellVarContext current
  IfFileExists "$INSTDIR\.shotout-nsis" 0 bad_uninstall
  FileOpen $0 "$INSTDIR\.shotout-nsis" r
  FileRead $0 $1
  FileClose $0
  StrCmp $1 "ShotOut.NSIS.1" valid_uninstall
  bad_uninstall:
  IfSilent no_warning
  MessageBox MB_OK|MB_ICONSTOP "This folder is not a recognized ShotOut NSIS installation. No files were removed."
  no_warning:
  SetErrorLevel 3
  Abort
  valid_uninstall:
  Call un.CheckNotRunning
FunctionEnd
Section "Uninstall"
  SetShellVarContext current
  Call un.CheckNotRunning
  Delete "$INSTDIR\ShotOut.exe"
  Delete "$INSTDIR\README.md"
  Delete "$INSTDIR\Shortcuts.md"
  Delete "$INSTDIR\Shortcuts.html"
  Delete "$INSTDIR\CHANGELOG.md"
  Delete "$INSTDIR\RELEASE-NOTES.md"
  Delete "$INSTDIR\SOUND-CREDITS.md"
  Delete "$INSTDIR\ShotOut-Brand-*.ico"
  Delete "$INSTDIR\.shotout-nsis"
  Delete "$INSTDIR\Uninstall.exe"
  Delete "${START_LINK}"
  Delete "${DESKTOP_LINK}"
  ReadRegStr $0 HKCU "${RUN_KEY}" "ShotOut"
  StrCmp $0 '$\"$INSTDIR\ShotOut.exe$\" --startup' 0 +2
  DeleteRegValue HKCU "${RUN_KEY}" "ShotOut"
  ReadRegStr $0 HKCU "${UNINSTALL_KEY}" "InstallLocation"
  StrCmp $0 "$INSTDIR" 0 +2
  DeleteRegKey HKCU "${UNINSTALL_KEY}"
  RMDir "$INSTDIR"
  DetailPrint "Your screenshots, projects, styles and settings have been kept."
  System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0, p 0, p 0)'
SectionEnd






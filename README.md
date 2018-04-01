# DownloadMAMERoms
Downloader for MAME ROMS

    Usage:
     dmr [/s] [/f] [/r auto|missing|all] [/c] /d

     /s Sections: A sequence of letters and or numbers indicating the ROMs sections to scan
     /f Filter: One or more words to filter the ROMs names to download
     /d Destination folder: Specifies the folder where the ROMs will be downloaded
     /r Download mode:
            auto:    Download only missing ROMs and ROMs with wrong file sizes
            missing: Download only missing ROMs ignoring the ROMs file sizes
            all:     Download all available ROMs
     /c Connections: Defines the maximum number of simultaneous connections for the scanning process

    Examples:
     1) Download all roms to c:\mame\roms
        dmr /d c:\mame\roms

     2) Download roms from sections 'Numbers', 'F' and 'G' to c:\emulators\mame\roms
        dmr /s #FG /d c:\emulators\mame\roms

     3) Download all roms from section 'G', containing the word 'galaga' to c:\emulators\mame\roms
        dmr /s G /f galaga /d c:\emulators\mame\roms

    Shortcut Keys:
     ESC: Abort process and terminate

# DownloadMAMERoms
Downloader for MAME ROMS

    Usage:
    dmr [/s] [/f] [/r] /d

    /s  Sections: A sequence of letters and or numbers indicating the ROMs sections to scan
    /f  Filter: One or more words to filter the ROMs names to download
    /r  Download all ROMs even if they already exist
    /d  Destination folder: Specifies the folder where the ROMs will be downloaded

    Examples:
     1) Download all roms to c:\mame\roms
        dmr /d c:\mame\roms

     2) Download roms from sections 'Numbers', 'F' and 'G' to c:\emulators\mame\roms
        dmr /s #FG /d c:\emulators\mame\roms

     3) Download all roms from section 'G', containing the word 'galaga' to c:\emulators\mame\roms
        dmr /s G /f galaga /d c:\emulators\mame\roms

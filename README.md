# DownloadMAMERoms
Downloader for MAME ROMS

Download MAME ROMs

    Usage:
    dmr [/s] [/f] /d

    /s  Sections
    /f  Filter
    /d  Destination folder

    Examples:
     1) Download all roms to c:\mame\roms
        dmr /d c:\mame\roms

     2) Download roms from sections 'Numbers', 'F' and 'G' to c:\emulators\mame\roms
        dmr /s #FG /d c:\emulators\mame\roms

     3) Download all roms from section 'G', containing the word 'galaga' to c:\emulators\mame\roms
        dmr /s G /f galaga /d c:\emulators\mame\roms

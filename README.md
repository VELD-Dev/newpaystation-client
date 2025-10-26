# NewPayStation Client
NewPayStation Client is an **unofficial**, lightweight, open-source, comfortable and easy-to-use client for NoPayStation.  
This was originally a project made for fun to use `Spectre.Console` C# library, but I ended finding it quite useful for myself, so I thought I would share it.

> [!IMPORTANT]
> This project is not affiliated with or endorsed by NoPayStation in any way.
> We also do not endorse piracy or the use of this client for any illegal activity.

## Features
- Load any TSV file from NoPayStation
- Search and filter by game titles, PSN ID, region, content type and availability
- View detailed information about each package
- Download package and RAP files
- Pause, resume, and manage multiple downloads
- Customizable download directory
- Not a CLI, but a Console GUI
- Lightweight and Cross-platform (Windows, macOS, Linux)

## Planned Features
- Update checker for new TSV files
- Proper settings
- Package extractor
- Better tracking of in-progress downloads between sessions

## Installation
### Prerequisites
- [.NET 9.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later.
> [!CAUTION]  
> A TSV file from [**NoPayStation**](https://nopaystation.com/). Make sure you download them **ONLY** from the official [**NoPayStation site**](https://nopaystation.com/). If someone provides you a TSV file from another source, it may contain malicious links or bad files.

### Windows
1. Download the latest release from the [Releases](https://github.com/VELD-Dev/newpaystation-client/releases/) page.
1. Extract the ZIP file.
1. Download a TSV file (PS3, PSP, PSVita, PSM, PSX, PSV...) from [**NoPayStation**](https://nopaystation.com/) **ONLY**.
1. Place it in the same directory as the executable.
1. Run `NewPayStation.Client.exe`

### Linux 
1. Download the latest release from the [Releases](https://gituhb.com/VELD-Dev/newpaystation-client/releases/) page.
1. Extract the TAR.GZ file.
1. Download a TSV file (PS3, PSP, PSVita, PSM, PSX, PSV...) from [**NoPayStation**](https://nopaystation.com/) **ONLY**.
1. Place it in the same directory as the executable.
1. Open a terminal in that directory and run:
   ```bash
   # Modify permissions to make it executable
   chmod +x NewPayStationClient
   ./NewPayStation.Client
   ```

### macOS
I don't have a macOS device to test this on, but you can try the Linux instructions above adapted to macOS commands.

## Attribution
- [**NoPayStation**](https://nopaystation.com/)
- [**`Spectre.Console`**](https://spectreconsole.net/)

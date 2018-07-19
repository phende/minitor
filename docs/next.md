# WIP and Wild Ideas

## Features

- Allow optional configuration file, format TBD
- Persistence, maybe using a provider pattern to store state in a key-value store (can public interfaces in an exe file be referenced from a library project ?)

## Internals

- Decouple web server code from HttpListener model, for cleanliness and future evolutivity
- Add minimal template processing for shared code between pages
- Modularize web handlers, with 'status' as the first module
- Self-host bootstrap, or minimum CSS, to remove requirement for internet access

## Documentation

- Code walkthrough
- Examples and integrations
    - C# updater sample
    - PowerShell Performance Counter sample
    - Real-life sample, maybe for Exchange
    - Bash system state sample

## Long shots

- REST/JSON interface to retrieve status lists from PowerShell and REST tools
- Self installing/deinstalling as a service on Windows
- Logging module, showing the last n received log entries
- Pin board/Chat module, for changes and issues in progress
- Separate module version for hosting in a web server (ASP.Net Core, Kestrel, IIS)

And your ideas here...

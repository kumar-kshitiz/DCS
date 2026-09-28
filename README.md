# Remote File Management System

A simple college mini-project that demonstrates client-server communication using classic .NET Remoting in a Windows + .NET Framework 4.8 environment.

## Project Overview

This project simulates a remote file management system with:

- Client console application
- Server console application
- Classic .NET Remoting over TCP
- File and directory operations
- User authentication with salted PBKDF2 password hashing
- Server-side authorization and path validation
- Logging and server metrics
- Basic ZIP compression and extraction

## Features

1. User registration and login
2. Password change
3. Upload, download, delete, rename, copy and move files
4. Read and write text files
5. File metadata reporting
6. File hashing using SHA-256
7. Directory creation and deletion
8. Directory listing and navigation within user root
9. File searching
10. ZIP archive creation and extraction with ZIP slip protection
11. Server information display
12. Activity logging
13. Remote method invocation through .NET Remoting

## Architecture

```text
┌──────────────────────────────┐
│ Client Console App           │
│ - Auth UI                    │
│ - File UI                    │
│ - Directory UI               │
│ - Server metrics UI          │
└──────────────┬───────────────┘
               │ TCP Remoting
               ▼
┌──────────────────────────────┐
│ Server Console App           │
│ - AuthService                │
│ - FileService                │
│ - DirectoryService           │
│ - SystemService              │
│ - PathSecurity               │
│ - ActivityLogger             │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│ User Storage                 │
│ Storage/Users/<username>/    │
│ Storage/Logs/activity.log    │
└──────────────────────────────┘
```

## Technologies

- C#
- .NET Framework 4.8
- Classic .NET Remoting
- TCP Channel
- MarshalByRefObject
- System.IO and System.IO.Compression
- PBKDF2 hashing with random salt

## Project Structure

```text
RemoteFileManagement/
├── RemoteFileManagement.sln
├── README.md
├── RemoteFileManagement.Shared/
│   ├── Interfaces/
│   ├── Models/
│   └── Enums/
├── RemoteFileManagement.Server/
│   ├── Services/
│   ├── Security/
│   ├── Storage/
│   ├── Program.cs
│   ├── Server.config
│   └── RemoteFileManagement.Server.csproj
└── RemoteFileManagement.Client/
    ├── Services/
    ├── UI/
    ├── Program.cs
    ├── Client.config
    └── RemoteFileManagement.Client.csproj
```

## Explanation of .NET Remoting

.NET Remoting is the classic Microsoft technology used for communicating between .NET objects over a network. In this project:

- The server hosts remote objects.
- The client creates a proxy to those objects.
- The server object inherits from MarshalByRefObject.
- Calls are sent over a TCP channel.
- This is a simple client-server demonstration for educational purposes.

## How MarshalByRefObject Works

A MarshalByRefObject is a .NET object whose lifetime is controlled by the remoting infrastructure rather than only by the local process. This allows a client to call methods on an object that actually exists on the server, while the client sees a transparent proxy.

## How Client-Server Communication Works

1. The server starts a TCP channel on a configured port.
2. Remote service types are registered with RemotingConfiguration.
3. The client registers a local TCP channel and creates proxies using Activator.GetObject.
4. Remote method calls are marshaled over the network.
5. The server performs the work and returns results as serializable DTOs or primitive values.

## Installation Requirements

### On Windows

- Visual Studio 2019 or 2022
- .NET Framework 4.8 Developer Pack
- Windows 10/11

### Current Linux environment

The project is being created for Windows execution, but the actual server/client cannot be run from this Linux environment because .NET Framework 4.8 and .NET Remoting are Windows-specific technologies.

Windows/.NET Framework execution could not be tested in the current Linux environment.

## Windows Setup

1. Open the solution in Visual Studio.
2. Restore NuGet dependencies if prompted.
3. Set the solution configuration to Debug or Release.
4. Ensure the .NET Framework 4.8 target is selected in each project.
5. Build the solution.

## How to Build

In Visual Studio:

1. Open RemoteFileManagement.sln.
2. Build -> Build Solution.
3. Confirm there are no project reference or target framework errors.

From command line on Windows, use:

```bat
msbuild RemoteFileManagement.sln /p:Configuration=Debug
```

## How to Start the Server

1. Open a Windows command prompt.
2. Run the server executable:

```bat
RemoteFileManagement.Server.exe
```

or from Visual Studio:

- Set RemoteFileManagement.Server as startup project.
- Press F5.

The server listens on the configured port from Server.config.

## How to Start the Client

1. Start the server first.
2. Run the client executable:

```bat
RemoteFileManagement.Client.exe
```

or from Visual Studio:

- Set RemoteFileManagement.Client as startup project.
- Press F5.

## Configuration

Configuration files are included:

- Server/Server.config
- Client/Client.config

Default values:

```xml
ServerHost: 127.0.0.1
RemotingPort: 9090
StorageRoot: Storage
LogDirectory: Storage/Logs
MaxUploadSizeBytes: 10485760
```

## Local Testing

For same-machine testing:

- Server and client run on the same Windows PC.
- Use localhost or 127.0.0.1.
- Default port is 9090.

Example login accounts:

- alice / alice123
- bob / bob123
- charlie / charlie123

## Two-Machine Testing

### Machine 1

- Windows server machine
- Example IP: 192.168.1.10

### Machine 2

- Windows client machine
- Update Client.config with the server IP.

Example:

```xml
<add key="ServerHost" value="192.168.1.10" />
<add key="RemotingPort" value="9090" />
```

## Firewall Configuration

Windows Firewall must allow incoming TCP traffic on port 9090.

### Steps

1. Open Windows Defender Firewall.
2. Go to Advanced Settings.
3. Create an inbound rule.
4. Select Port.
5. Choose TCP.
6. Enter 9090.
7. Allow the connection.
8. Apply to all network profiles.

## Example Usage

### Register a user

- Username: alice
- Password: alice123

### Login

- User: alice
- Password: alice123

### Upload file

- Select a local file.
- The file is stored under the authenticated user's storage path.

### Download file

- Enter the remote file path.
- The file is saved locally.

## Security mechanisms

- Passwords are never stored in plain text.
- PBKDF2 is used with random salt.
- Paths are constrained to the user's root directory.
- Path traversal is blocked.
- Invalid Windows filenames are rejected.
- ZIP extraction checks for ZIP slip attempts.
- Server-side authorization is enforced.
- Clean error messages are returned without stack traces to the client.

## OOP Concepts Demonstrated

- Encapsulation: service classes and validation logic are separated.
- Abstraction: remote interfaces define service contracts.
- Polymorphism: different service implementations can be used through interfaces.
- Interfaces: shared contracts describe remote capabilities.
- Composition: server aggregates services with a shared configuration and logging layer.
- Single Responsibility Principle: file operations, auth, directory actions, and system monitoring are separated.

## Limitations

- This is a college mini-project, not a production-grade enterprise system.
- Only basic security controls are implemented.
- No encrypted transport beyond the legacy remoting channel.
- No real multi-user concurrent session management beyond a basic logged-in set.
- No GUI; the client is a console interface.

## Future Improvements

- Add database-backed user storage.
- Add a richer interactive Windows Forms UI.
- Add more robust session management.
- Add file versioning and quotas.
- Add stronger cryptography, TLS, and encryption.

## Main Operations in the Client Menu

1. Upload File
2. Download File
3. Delete File
4. Rename File
5. Copy / Move File
6. Read / Write Text File
7. File Information
8. Calculate File Hash
9. Create Directory
10. Delete Directory
11. List Files & Directories
12. Search Files
13. Compress / Extract ZIP
14. Server Information
15. Activity Logs
16. Logout
17. Exit

## Viva Notes

This mini-project is suitable for a viva because it demonstrates:

- client-server design
- .NET Remoting architecture
- remote method invocation
- serialization through DTOs
- authentication
- file operations
- security validation
- logging
- OOP structure

## Important Note

The project targets Windows + .NET Framework 4.8 + classic .NET Remoting, as requested. It is intentionally not migrated to ASP.NET Core, WCF, REST, gRPC, or any modern stack.

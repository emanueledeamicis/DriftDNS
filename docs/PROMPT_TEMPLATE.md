# Prompt Template for Coding Agent

This template should be used when interacting with the coding agent.

It ensures the agent follows the project architecture and produces consistent code.

---

# Context

You are working on the **DriftDNS** project.

DriftDNS is an open-source **self-hosted dynamic DNS manager** designed for homelab users and developers who run services on networks with dynamic public IP addresses.

The application automatically detects the host's public IP and updates DNS records on supported DNS providers.

The application:

- runs in a **single Docker container**
- is built with **ASP.NET Core**
- uses **Blazor Server** for the web dashboard
- uses **SQLite with Entity Framework Core**
- includes a **background worker that synchronizes DNS records**
- supports **multiple DNS providers through a provider abstraction layer**

Project documentation is available in:

/docs/AI_PROJECT_BRIEF.md  
/docs/ARCHITECTURE.md  
/docs/INITIAL_TASKS.md  

Always read and follow those documents before implementing new code.

---

# Coding Guidelines

Always follow these rules when writing code.

1. Use dependency injection for all services.
2. Prefer async methods for I/O operations.
3. Create interfaces for services.
4. Keep classes small and focused.
5. Avoid tightly coupled code.
6. Follow the layered architecture defined in the architecture document.
7. Ensure new code integrates cleanly with existing services.

Never introduce unnecessary complexity.

---

# Task

Clearly describe the implementation task.

Example:

Implement the `PublicIpResolver` service that detects the public IP address of the host machine.

---

# Functional Requirements

List the functional requirements the implementation must satisfy.

Example:

- must call multiple public IP detection services
- must validate returned IP address
- must support IPv4
- must handle network errors gracefully
- must use HttpClient with dependency injection

---

# Expected Output

When generating code:

- show **complete files**
- specify **file paths**
- include **namespaces**
- ensure the code **compiles**
- follow the project naming conventions

Example output format:

File:  
src/DriftDNS.Core/Services/PublicIpResolver.cs

---

# Integration Rules

When adding code:

- interfaces must be placed in **DriftDNS.Core**
- domain models must be placed in **DriftDNS.Core**
- EF Core and database logic must be placed in **DriftDNS.Infrastructure**
- DNS provider implementations must be placed in **DriftDNS.Providers.\***

Examples:

DriftDNS.Providers.Route53  
DriftDNS.Providers.Cloudflare

---

# Validation Step

After generating code:

1. Verify the implementation matches the architecture.
2. Ensure dependencies are correctly registered in dependency injection.
3. Ensure async patterns are used where needed.
4. Confirm the code compiles.

---

# Example Task Prompt

Implement the `PublicIpResolver` service.

Requirements:

- service must implement `IPublicIpResolver`
- must try multiple IP detection services
- must validate returned IP
- must return the IP as string
- must log failures
- must use HttpClient injected via dependency injection

Place implementation in:

DriftDNS.Infrastructure
# Initial Development Tasks

This document describes the first development tasks for the coding agent.

The goal is to bootstrap the project structure and implement the first working MVP.

---

# Phase 1 – Project Setup

Create the solution structure.

Solution name:

DynamicDns

Project structure:

DynamicDns.sln

src/
  DynamicDns.App
  DynamicDns.Core
  DynamicDns.Infrastructure
  DynamicDns.Providers.Route53

tests/
  DynamicDns.Tests

---

# Phase 2 – Core Domain Models

Create the following domain models.

## ProviderAccount

Represents credentials for a DNS provider.

Fields:

- Id (Guid)
- Name (string)
- ProviderType (string)
- EncryptedCredentials (string)
- IsEnabled (bool)
- CreatedAt (DateTime)

---

## DnsEndpoint

Represents a DNS record that should be automatically updated.

Fields:

- Id (Guid)
- Hostname (string)
- ZoneName (string)
- RecordType (string)
- ProviderAccountId (Guid)
- TTL (int)
- Enabled (bool)
- CreatedAt (DateTime)

Example hostname:

home.example.com

---

## SyncState

Stores last synchronization state.

Fields:

- EndpointId (Guid)
- LastKnownPublicIp (string)
- LastAppliedIp (string)
- LastCheckAt (DateTime)
- LastSuccessAt (DateTime)
- LastError (string)

---

## SyncLog

Stores historical sync logs.

Fields:

- Id (Guid)
- EndpointId (Guid)
- Timestamp (DateTime)
- OldIp (string)
- NewIp (string)
- Action (string)
- Details (string)

Possible Action values:

NoChange  
Updated  
Failed  

---

# Phase 3 – Core Interfaces

Create the following interfaces.

IDnsProvider

Responsibilities:

- validate credentials
- update DNS records

IPublicIpResolver

Responsibilities:

- detect current public IP

IDnsSyncService

Responsibilities:

- orchestrate synchronization process

---

# Phase 4 – Database

Use:

Entity Framework Core  
SQLite

Create DbContext:

DynamicDnsDbContext

DbSets:

- ProviderAccounts
- DnsEndpoints
- SyncStates
- SyncLogs

Enable EF Core migrations.

---

# Phase 5 – Public IP Resolver

Create service:

PublicIpResolver

Strategy:

Call multiple public IP services:

https://checkip.amazonaws.com  
https://api.ipify.org  

Steps:

1. call first service
2. validate response
3. fallback to second if needed
4. return IP address

---

# Phase 6 – DNS Sync Engine

Create service:

DnsSyncService

Responsibilities:

1. resolve public IP
2. load enabled endpoints
3. compare IP with last applied IP
4. if IP changed → update DNS provider
5. write logs
6. update sync state

Method example:

RunSyncAsync()

---

# Phase 7 – Background Worker

Create class:

DnsSyncWorker : BackgroundService

Loop behavior:

1. wait configured interval
2. call DnsSyncService.RunSyncAsync()
3. handle errors
4. log results

Default interval:

5 minutes

Make interval configurable.

---

# Phase 8 – Route53 Provider

Create project:

DynamicDns.Providers.Route53

Create class:

Route53DnsProvider

Use AWS SDK for .NET.

Implement:

IDnsProvider

Required operations:

- validate credentials
- upsert DNS record

Use Route53 ChangeResourceRecordSets API.

---

# Phase 9 – Blazor UI

Create basic dashboard.

Pages required:

## Overview

Show:

- current detected public IP
- total endpoints
- last sync time

---

## Endpoints

CRUD page.

Fields:

- hostname
- provider
- record type
- TTL
- enabled

---

## Providers

CRUD page.

Example fields for Route53:

- AccessKey
- SecretKey
- Region

---

## Logs

Table showing:

- timestamp
- endpoint
- action
- message

---

# Phase 10 – Docker Support

Create Dockerfile.

Requirements:

- build .NET application
- publish app
- run ASP.NET server
- expose port 8080

SQLite database stored in:

/app/data/app.db

This directory must support Docker volumes.

---

# MVP Completion Criteria

The MVP is complete when:

- application starts successfully
- Blazor dashboard loads
- endpoints can be created
- Route53 provider works
- IP change triggers DNS update
- logs are visible
- container runs correctly in Docker
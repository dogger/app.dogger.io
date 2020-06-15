---
slug: "/documentation"
title: Documentation
---

# Pull Dog

## Quickstart

### Installing the app
The first thing to do is to <a href="https://github.com/apps/pull-dog/installations/new">install the GitHub app</a> to your user and/or organizations.

### Enabling Pull Dog for a repository
To enable Pull Dog for a repository, you need to push a `pull-dog.json` configuration file to the repository's `master` branch.

The most minimal configuration possible contains just a pointer to the `docker-compose.yml` file in your repository that you want to provision a test environment from:

```
{
    "dockerComposeYmlFilePaths": [
        "your/path/to/docker-compose.yml"
    ]
}
```

_For more ways to customize Pull Dog or how it builds your test environment, see <a href="#configuration">Configuration</a>._

## Configuration
All below configuration values can be set either using a <a href="#lazy-configuration">lazy configuration</a> or a <a href="#json-file-configuration">JSON file configuration</a>.

|Name &amp; description|Example|Type|Default|
|----------------------|-------|----|-------|
|`expiry` - sets a timeout on when the environment should be destroyed again.|`"2.03:02:10"` for 2 days, 3 hours, 2 minutes and 10 seconds|`string?`|`null`|
|`buildArguments` - specifies build-time environment variables and build arguments.|`{ MY_KEY: "my value" }`|`{ [argumentName: string]: string }`|`{ }`|
|`conversationMode` - specifies how comments from Pull Dog should behave.|`"singleComment"|"multipleComments"`|`string?`|`"singleComment"`|
|`dockerComposeYmlFilePaths` - tells Pull Dog which Docker Compose YML file(s) to choose when provisioning a test environment.|`["your/path/to/docker-compose.yml"]`|`string[]`|`null`|

### JSON file configuration
The simplest way of configuring your 

### Lazy configuration

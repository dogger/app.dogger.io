---
slug: "/blog/6-reasons-why-docker-is-used-in-2020"
date: "2020-04-29"
title: "6 Reasons Why Docker Is Used in 2020"
summary: "In this blog post, I’ll go through the top reasons I have seen people use Docker throughout my career."
---

Docker is a tool everyone is using nowadays, but what are they using Docker for?

In this blog post, I’ll go through the top reasons I have seen people use Docker throughout my career.

![Most people have seen the iconic Docker whale "Moby"](https://miro.medium.com/max/672/1*glD7bNJG3SlO0_xNmSGPcQ.png)

### 1. It’s clean
A Docker application runs inside a container. This container is isolated from the host machine, and therefore can’t leave a mess behind.

Take Microsoft SQL Server for instance. In the past, if you were to use that, you’d have to install Microsoft SQL Server on the machine. Other than “polluting” your system with a dozen registry entries and files, it would also install a Windows service that would run in the background, even when you were not using Microsoft SQL Server.

Most Windows users are also familiar with cases where installing a specific program and uninstalling it again, still leaves files and various registry entries behind.

Docker solves these issues. Need your application? Start it up. Don’t need it anymore? Tear it down or delete it. No traces will be left behind on the host machine.

### 2. It works on their machine too
Docker containers run on Linux, even if your host machine is Windows or Mac. This allows for your Docker application to be fully cross-platform.

They also run the same on any host machine, no matter how polluted or different this host machine is.

![The origin of Docker](https://miro.medium.com/max/844/1*2IWD6fLgoe0oMKvAD6CUqQ.jpeg)

Let’s say your colleague is running into issues while testing your application. It could be because his/her installed SQL Server is of a different version, or that he/she is running on a different OS version. Perhaps he/she didn’t even install SQL Server on their machine.

Docker solves these issues. You don’t have to install anything except Docker. Everything else is handled for you. If it works within the container, it’s as if it would work on a brand new and clean machine as well.

### 3. It can make testing easy
Often you need to test your app towards real dependencies, like a real SQL Server. For instance, it may be a manual test of your full application locally on your machine, or it may be that you’re writing an integration test where faking and mocking a SQL Server isn’t enough.

In addition, you might want someone else to easily test the work you’ve done or a working on (perhaps someone from the Quality Assurance team). For that, there are GitHub apps like Pull Dog which uses Docker to automatically set up a real test environment in the cloud for each pull request, as they are opened.

### 4. It can deploy your application
Because Docker has very little overhead in comparison to the features it offers, it is often used on servers in the cloud as well.

Since Docker is platform agnostic and doesn’t care what host machine it runs on, your application won’t depend on a specific host or cloud provider — you can simply change that later, and your application will still run the same way (assuming it is a <a rel="nofollow" target="_blank" href="https://12factor.net/">12-factor app</a>).

Once you have your application running locally, there are various tools for getting it up and running in the cloud. For more advanced use cases, there’s <a href="https://kubernetes.io/" rel="nofollow" target="_blank">Kubernetes</a>.

*If you want to get started quickly though*, or are building a small-to-medium-sized application, there are things like <a href="/">Dogger</a>, which make it easier to host your application without worrying about pushing its image somewhere first.

<img src="/images/dogger-no-title.svg" alt="Dogger logo" />

### 5. It can build your application
Docker is used heavily on build servers during the compilation of your app or while publishing it since it allows you to not rely on what software is installed on the build server agents.

For instance, it would be a shame if your build process needs to run `npm install` to install its Node dependencies if it doesn’t have Node installed. Most build systems have Docker installed, and inside Docker, you can do anything you want.

By moving the build process to Docker, you also don’t rely on a specific build server type. Moving from GitHub Actions to BitBucket Pipelines or TeamCity? No problem. They all run Docker.

### 6. It allows for more control
Applications running within Docker containers can decide not to expose any ports to the host machine, or perhaps even remap the ports.

For instance, with Docker, it is very easy to have two SQL Server containers running side-by-side on the same host machine, each with a different port, without diving into the SQL Server’s settings.

Additionally, each container can be constrained in terms of its maximum allowed CPU and memory usage. This could be useful in cases where you have some dependencies of your application that you’d want to give less processing power, ensuring that it doesn’t consume all of it.
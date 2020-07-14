using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Dogger.Infrastructure.Docker.Yml
{

    public class DockerComposeParser : IDockerComposeParser
    {
        private readonly JsonDocument? dockerComposeModel;

        public DockerComposeParser(string dockerComposeYmlContents)
        {
            try
            {
                using var reader = new StringReader(dockerComposeYmlContents);

                var deserializer = new DeserializerBuilder().Build();
                var yamlObject = deserializer.Deserialize(reader);
                if (yamlObject == null)
                    return;

                var serializer = new SerializerBuilder()
                    .JsonCompatible()
                    .Build();

                var json = serializer.Serialize(yamlObject);
                this.dockerComposeModel = JsonDocument.Parse(json);
            }
            catch (SyntaxErrorException ex)
            {
                throw new DockerComposeSyntaxErrorException(ex);
            }
        }

        public IReadOnlyCollection<string> GetEnvironmentFilePaths()
        {
            var result = new List<string>();
            foreach (var environmentFileElement in GetServiceElementProperties("env_file"))
            {
                if (environmentFileElement.ValueKind == JsonValueKind.String)
                {
                    result.Add(environmentFileElement.GetString());
                }
                else if (environmentFileElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in environmentFileElement.EnumerateArray())
                        result.Add(element.GetString());
                }
            }

            return result;
        }

        public IReadOnlyCollection<string> GetVolumePaths()
        {
            var globalVolumeNames = 
                GetRootProperty("volumes")
                    ?.EnumerateObject()
                    .Select(x => x.Name)
                    .ToList() ??
                new List<string>();

            var result = new List<string>();
            foreach (var environmentFileElement in GetServiceElementProperties("volumes"))
            {
                if (environmentFileElement.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var valueElement in environmentFileElement.EnumerateArray())
                {
                    if (valueElement.ValueKind == JsonValueKind.String)
                    {
                        var reference = valueElement
                            .GetString()
                            .Split(':', 2)[0];
                        if (globalVolumeNames.Contains(reference))
                            continue;

                        result.Add(reference);
                    }
                    else if (valueElement.ValueKind == JsonValueKind.Object)
                    {
                        var type = valueElement
                            .GetProperty("type")
                            .GetString();
                        if (type != "bind")
                            continue;

                        result.Add(valueElement
                            .GetProperty("source")
                            .GetString());
                    }
                }
            }

            return result;
        }

        public IReadOnlyCollection<string> GetDockerfilePaths()
        {
            var result = new List<string>();
            foreach (var environmentFileElement in GetServiceElementProperties("build"))
            {
                if (environmentFileElement.ValueKind == JsonValueKind.Object)
                {
                    var foundDockerfileReference = false;
                    foreach (var property in environmentFileElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.String)
                            continue;

                        if (property.Name == "dockerfile")
                        {
                            foundDockerfileReference = true;
                            result.Add(property.Value.GetString());
                        }
                    }

                    if (!foundDockerfileReference)
                    {
                        result.Add("Dockerfile");
                    }
                } else if (environmentFileElement.ValueKind == JsonValueKind.String)
                {
                    var contextPath = environmentFileElement.GetString();

                    var dockerfilePath = Path
                        .Join(
                            contextPath,
                            "Dockerfile")
                        .Replace(
                            "\\", "/", 
                            StringComparison.InvariantCulture);

                    result.Add(dockerfilePath);
                }
            }

            return result;
        }

        private IReadOnlyCollection<JsonElement> GetServiceElementProperties(string propertyName)
        {
            return GetServiceElements()
                .Where(x => x.TryGetProperty(propertyName, out _))
                .Select(x => x.GetProperty(propertyName))
                .Where(x =>
                    x.ValueKind != JsonValueKind.Null &&
                    x.ValueKind != JsonValueKind.Undefined)
                .ToArray();
        }

        private JsonElement? GetRootProperty(string name)
        {
            if (dockerComposeModel == null || !dockerComposeModel.RootElement.TryGetProperty(name, out var result))
                return null;

            return result;
        }

        private IReadOnlyCollection<JsonElement> GetServiceElements()
        {
            var element = GetRootProperty("services");
            if (element == null)
                return Array.Empty<JsonElement>();

            if (element.Value.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Expected services element to contain an object.");

            return element
                .Value
                .EnumerateObject()
                .Select(x => x.Value)
                .Where(x => x.ValueKind == JsonValueKind.Object)
                .ToArray();
        }

        public IReadOnlyCollection<ExposedPort> GetExposedHostPorts()
        {
            var result = new List<ExposedPort>();

            foreach (var ports in GetServiceElementProperties("ports"))
            {
                if (ports.ValueKind == JsonValueKind.Null || ports.ValueKind == JsonValueKind.Undefined)
                    continue;

                if (ports.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException("The ports were not of type array.");

                foreach (var port in ports.EnumerateArray())
                {
                    if (port.ValueKind == JsonValueKind.String)
                    {
                        var stringValue = port.GetString();
                        result.AddRange(GetExposedHostPortsFromEntry(stringValue));
                    }
                    else if (port.ValueKind == JsonValueKind.Object)
                    {
                        var protocol = SocketProtocol.Tcp;
                        int? portNumber = null;

                        foreach (var portProperty in port.EnumerateObject())
                        {
                            switch (portProperty.Name)
                            {
                                case "published":
                                    portNumber = int.Parse(
                                        portProperty.Value.GetString(),
                                        CultureInfo.InvariantCulture);
                                    break;

                                case "protocol":
                                    var protocolString = portProperty.Value.GetString();
                                    if (protocolString == "udp")
                                    {
                                        protocol = SocketProtocol.Udp;
                                    }
                                    break;
                            }
                        }

                        if (portNumber == null)
                            throw new InvalidOperationException("Could not find port number.");

                        result.Add(new ExposedPort()
                        {
                            Port = portNumber.Value,
                            Protocol = protocol
                        });
                    }
                    else
                    {
                        throw new InvalidOperationException("The port was not of the right type.");
                    }
                }
            }

            return result.ToArray();
        }

        private static IReadOnlyCollection<ExposedPort> GetExposedHostPortsFromEntry(string stringValue)
        {
            var protocolSplit = stringValue.Split('/');

            SocketProtocol? GetProtocol()
            {
                if (protocolSplit!.Length > 1 && protocolSplit[1] == "udp")
                    return SocketProtocol.Udp;

                return SocketProtocol.Tcp;
            }

            IReadOnlyCollection<int> GetPortsFromRange(string range)
            {
                var rangeSplit = range.Split('-');
                if (rangeSplit.Length == 1)
                {
                    return new[] {
                        int.Parse(rangeSplit[0], CultureInfo.InvariantCulture)
                    };
                }
                else if (rangeSplit.Length == 2)
                {
                    var lowerLimit = int.Parse(rangeSplit[0], CultureInfo.InvariantCulture);
                    var upperLimit = int.Parse(rangeSplit[1], CultureInfo.InvariantCulture);

                    var ports = new List<int>();
                    for (var port = lowerLimit; port <= upperLimit; port++)
                    {
                        ports.Add(port);
                    }

                    return ports;
                }

                throw new InvalidOperationException("Invalid port range with more than 1 dash delimiter.");
            }

            IReadOnlyCollection<int> GetPorts()
            {
                var portSplit = protocolSplit![0].Split(':');
                if (portSplit.Length <= 2)
                {
                    return GetPortsFromRange(portSplit[0]);
                }
                else if (portSplit.Length == 3)
                {
                    return GetPortsFromRange(portSplit[1]);
                }

                throw new InvalidOperationException("Unknown port format with more than 2 colon delimiters.");
            }

            var protocol = GetProtocol();
            if (protocol == null)
                return Array.Empty<ExposedPort>();

            return GetPorts()
                .Select(port => new ExposedPort()
                {
                    Protocol = protocol.Value,
                    Port = port
                })
                .ToArray();
        }
    }
}

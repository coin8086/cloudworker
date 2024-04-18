using System.Reflection;
using System.Runtime.Loader;
using System;
using Microsoft.Extensions.Logging;

namespace CloudWorker.ServiceHost;

class ServiceAssemblyLoadContext : AssemblyLoadContext
{
    private AssemblyDependencyResolver _resolver;

    private ILogger? _logger;

    public ServiceAssemblyLoadContext(string pluginPath, ILogger? logger = null)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _logger = logger;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        _logger?.LogDebug("Try to load assembly '{name}'", assemblyName.FullName);

        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            _logger?.LogDebug("Resolved assembly path '{path}'", assemblyPath);
            return LoadFromAssemblyPath(assemblyPath);
        }

        _logger?.LogDebug("Resolved no path for assembly.");
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        _logger?.LogDebug("Try to load unmanaged assembly '{name}'", unmanagedDllName);

        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            _logger?.LogDebug("Resolved assembly path '{path}'", libraryPath);
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        _logger?.LogDebug("Resolved no path for assembly.");
        return IntPtr.Zero;
    }
}

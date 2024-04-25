using System.Reflection;
using System.Runtime.Loader;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CloudWorker.ServiceHost;

class ServiceAssemblyLoadContext : AssemblyLoadContext
{
    private AssemblyDependencyResolver _resolver;

    private ISet<string>? _sharedAssemblyNames;

    private ILogger? _logger;

    public ServiceAssemblyLoadContext(string pluginPath, Type[]? sharedTypes = null, ILogger? logger = null)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _logger = logger;
        if (sharedTypes != null)
        {
            _sharedAssemblyNames = GetSharedAssemblyNames(sharedTypes);
        }
    }

    private static ISet<string> GetSharedAssemblyNames(Type[] sharedTypes)
    {
        var names = new HashSet<string>();
        foreach (var type in sharedTypes)
        {
            var assembly = type.Assembly;
            names.Add(assembly.GetName().Name!);
        }
        return names;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        _logger?.LogDebug("Try to load assembly '{name}'", assemblyName.FullName);

        if (_sharedAssemblyNames != null && _sharedAssemblyNames.Contains(assemblyName.Name!))
        {
            _logger?.LogDebug("Skip shared assembly '{name}'", assemblyName.Name);
            return null;
        }

        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            _logger?.LogDebug("Resolved assembly path '{path}'", assemblyPath);
            return LoadFromAssemblyPath(assemblyPath);
        }

        _logger?.LogDebug("Resolved no path for {assembly}.", assemblyName.Name);
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

        _logger?.LogDebug("Resolved no path for {assembly}.", unmanagedDllName);
        return IntPtr.Zero;
    }
}

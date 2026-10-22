namespace Gallean.Client;

internal sealed class InstalledAssetPackNotEnhancedException()
    : InstalledAssetPackException("The asset pack is a base pack without enhanced art.", innerException: null);

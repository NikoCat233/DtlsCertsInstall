using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Hazel.Dtls;
using Il2CppSystem.Security.Cryptography.X509Certificates;
using InnerNet;
using System;
using System.Collections.Generic;

namespace DtlsCertsInstall;

[BepInPlugin("me.niko233.certsinstall", "DtlsCertsInstall", "1.0.0")]
[BepInProcess("Among Us.exe")]
public partial class DtlsCertsInstallPlugin : BasePlugin
{
    internal static ManualLogSource Logger;

    public Harmony Harmony { get; } = new("me.niko233.certsinstall");

    public static List<X509Certificate2> Certificates = [];

    public static ConfigEntry<bool> ClearOfficialCert;
    private static ConfigEntry<string> CertificatesConfig;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo("Loading DtlsCertsInstallPlugin");

        // Configuration settings
        CertificatesConfig = Config.Bind("Certificates", "CertList", "\r\n-----BEGIN CERTIFICATE-----\r\nMIIDbTCCAlWgAwIBAgIUf8xD1G/d5NK1MTjQAYGqd1AmBvcwDQYJKoZIhvcNAQEL\r\nBQAwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgMClNvbWUtU3RhdGUxITAfBgNVBAoM\r\nGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDAgFw0yMTAyMDIxNzE4MDFaGA8yMjk0\r\nMTExODE3MTgwMVowRTELMAkGA1UEBhMCQVUxEzARBgNVBAgMClNvbWUtU3RhdGUx\r\nITAfBgNVBAoMGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDCCASIwDQYJKoZIhvcN\r\nAQEBBQADggEPADCCAQoCggEBAL7GFDbZdXwPYXeHWRi2GfAXkaLCgxuSADfa1pI2\r\nvJkvgMTK1miSt3jNSg/o6VsjSOSL461nYmGCF6Ho3fMhnefOhKaaWu0VxF0GR1bd\r\ne836YWzhWINQRwmoVD/Wx1NUjLRlTa8g/W3eE5NZFkWI70VOPRJpR9SqjNHwtPbm\r\nKi41PVgJIc3m/7cKOEMrMYNYoc6E9ehwLdJLQ5olJXnMoGjHo2d59hC8KW2V1dY9\r\nsacNPUjbFZRWeQ0eJ7kbn8m3a5EuF34VEC7DFcP4NCWWI7HO5/KYE+mUNn0qxgua\r\nr32qFnoaKZr9dXWRWJSm2XecBgqQmeF/90gdbohNNHGC/iMCAwEAAaNTMFEwHQYD\r\nVR0OBBYEFAJAdUS5AZE3U3SPQoG06Ahq3wBbMB8GA1UdIwQYMBaAFAJAdUS5AZE3\r\nU3SPQoG06Ahq3wBbMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggEB\r\nALUoaAEuJf4kQ1bYVA2ax2QipkUM8PL9zoNiDjUw6ZlwMFi++XCQm8XDap45aaeZ\r\nMnXGBqIBWElezoH6BNSbdGwci/ZhxXHG/qdHm7zfCTNaLBe2+sZkGic1x6bZPFtK\r\nZUjGy7LmxsXOxqGMgPhAV4JbN1+LTmOkOutfHiXKe4Z1zu09mOo9sWfGCkbIyERX\r\nQQILBYSIkg3hU4R4xMOjvxcDrOZja6fSNyi2sgidTfe5OCKC2ovU7OmsQqzb7mFv\r\ne+7kpIUp6AZNc49n6GWtGeOoL7JUAqMOIO+R++YQN7/dgaGDPuu0PpmgI2gPLNW1\r\nZwHJ755zQQRX528xg9vfykY=\r\n-----END CERTIFICATE-----\r\n",
            "List of certs to attach. Each cert will be resolved with CryptoHelpers.DecodePEM() and should split with comma(,)");

        ClearOfficialCert = Config.Bind("Settings", "ClearOfficialCert", false,
            "Clear InnerSloth's cert and only use certs loaded by plugin");

        // Load certificates
        LoadCertificatesFromConfig();

        Harmony.PatchAll();
    }

    private void LoadCertificatesFromConfig()
    {
        Certificates.Clear();
        var configValue = CertificatesConfig.Value;

        if (string.IsNullOrWhiteSpace(configValue))
        {
            Logger.LogInfo("No certificate list configured");
            return;
        }

        var certificateEntries = configValue.Split(',');
        int loadedCount = 0;
        int failedCount = 0;

        for (int i = 0; i < certificateEntries.Length; i++)
        {
            var entry = certificateEntries[i];
            if (string.IsNullOrWhiteSpace(entry)) continue;

            try
            {
                string certPem = entry.Trim();

                // Use PEM format directly as it includes complete format (\r\n at start and end)
                var cert = new X509Certificate2(CryptoHelpers.DecodePEM(certPem));
                Certificates.Add(cert);
                loadedCount++;

                Logger.LogInfo($"Successfully loaded certificate (Subject: {cert.Subject})");
            }
            catch (Exception ex)
            {
                failedCount++;
                Logger.LogError($"Failed to load certificate at index {i}: {ex.Message}");
                Logger.LogDebug($"Certificate content: {entry}");
            }
        }

        Logger.LogInfo($"Certificate loading completed: {loadedCount} successful, {failedCount} failed");
    }
}

[HarmonyPatch(typeof(DtlsUnityConnection), nameof(DtlsUnityConnection.SetValidServerCertificates))]
public static class SetValidServerCertificates_Patch
{
    // In 17.0.0 AuthManager.CreateDtlsConnection is inlined.
    // We have to patch the method that directly sets the cert to make dtls stuffs work.
    public static bool Prefix([HarmonyArgument(0)] ref X509Certificate2Collection certificateCollection)
    {
        // Clear official certificates if configured to do so
        if (DtlsCertsInstallPlugin.ClearOfficialCert?.Value == true)
        {
            certificateCollection.Clear();
            DtlsCertsInstallPlugin.Logger?.LogInfo("Official certificates cleared");
        }

        // Add custom certificates
        foreach (var cert in DtlsCertsInstallPlugin.Certificates)
        {
            certificateCollection.Add(cert);
        }

        DtlsCertsInstallPlugin.Logger?.LogInfo($"Certificate collection contains {certificateCollection.Count} certificates");

        return true;
    }
}

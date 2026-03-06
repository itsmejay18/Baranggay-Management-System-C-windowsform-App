using System;
using System.Collections.Generic;

namespace baranggaysystem1.helper;

internal static class WorkflowRules
{
    private static readonly HashSet<(string From, string To)> CertificateTransitions = new()
    {
        ("DRAFT", "SUBMITTED"),
        ("DRAFT", "CANCELLED"),
        ("DRAFT", "REJECTED"),
        ("SUBMITTED", "APPROVED"),
        ("SUBMITTED", "CANCELLED"),
        ("SUBMITTED", "REJECTED"),
        ("APPROVED", "RELEASED"),
        ("APPROVED", "CANCELLED"),
        ("REJECTED", "SUBMITTED")
    };

    private static readonly HashSet<(string From, string To)> BlotterTransitions = new()
    {
        ("ONGOING", "ONGOING"),
        ("ONGOING", "SETTLED"),
        ("ONGOING", "REFERRED"),
        ("SETTLED", "SETTLED"),
        ("SETTLED", "CLOSED"),
        ("REFERRED", "REFERRED"),
        ("REFERRED", "CLOSED"),
        ("CLOSED", "CLOSED")
    };

    internal static string NormalizeCertificateStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "SUBMITTED";
        }

        return status.Trim().ToUpperInvariant() switch
        {
            "REQUESTED" => "SUBMITTED",
            "ISSUED" => "RELEASED",
            _ => status.Trim().ToUpperInvariant()
        };
    }

    internal static string NormalizeBlotterStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "ONGOING";
        }

        return status.Trim().ToUpperInvariant() switch
        {
            "OPEN" => "ONGOING",
            _ => status.Trim().ToUpperInvariant()
        };
    }

    internal static bool TryValidateCertificateTransition(string? fromStatus, string? toStatus, out string message)
    {
        string from = NormalizeCertificateStatus(fromStatus);
        string to = NormalizeCertificateStatus(toStatus);

        if (from == to)
        {
            message = string.Empty;
            return true;
        }

        if (CertificateTransitions.Contains((from, to)))
        {
            message = string.Empty;
            return true;
        }

        message = $"Invalid certificate transition: {from} -> {to}.";
        return false;
    }

    internal static bool TryValidateBlotterTransition(string? fromStatus, string? toStatus, out string message)
    {
        string from = NormalizeBlotterStatus(fromStatus);
        string to = NormalizeBlotterStatus(toStatus);

        if (BlotterTransitions.Contains((from, to)))
        {
            message = string.Empty;
            return true;
        }

        message = $"Invalid blotter transition: {from} -> {to}.";
        return false;
    }

    internal static bool TryValidateNewBlotterStatus(string? status, out string message)
    {
        string normalized = NormalizeBlotterStatus(status);
        if (normalized == "ONGOING")
        {
            message = string.Empty;
            return true;
        }

        message = "New blotter records must start with Ongoing status.";
        return false;
    }
}

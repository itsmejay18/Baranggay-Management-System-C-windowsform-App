using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Confuser.Renamer.BAML;

namespace BamlToXaml;

/// <summary>
/// Converts a BamlDocument (parsed BAML records) into XAML text.
/// Based on the BAML binary format specification.
/// </summary>
public static class BamlToXamlConverter
{
    // Known WPF assembly short names
    private static readonly Dictionary<string, string> KnownAssemblies = new()
    {
        { "PresentationFramework", "PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" },
        { "PresentationCore", "PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" },
        { "WindowsBase", "WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" },
    };

    // Known WPF xmlns mappings
    private static readonly Dictionary<string, string> KnownXmlns = new()
    {
        { "http://schemas.microsoft.com/winfx/2006/xaml/presentation", "http://schemas.microsoft.com/winfx/2006/xaml/presentation" },
        { "http://schemas.microsoft.com/winfx/2006/xaml", "http://schemas.microsoft.com/winfx/2006/xaml" },
        { "http://schemas.microsoft.com/winfx/2006/xaml/presentation/options", "http://schemas.microsoft.com/winfx/2006/xaml/presentation/options" },
        { "http://schemas.openxmlformats.org/markup-compatibility/2006", "http://schemas.openxmlformats.org/markup-compatibility/2006" },
    };

    // Known type IDs for built-in WPF types (negative IDs)
    private static readonly Dictionary<short, (string Namespace, string Name)> KnownTypes = new()
    {
        { -1, ("System.Windows", "DependencyObject") },
        { -2, ("System.Windows", "DependencyProperty") },
        { unchecked((short)0xFFFC), ("System.Windows.Controls", "Grid") },
    };

    public static string Convert(BamlDocument doc)
    {
        var ctx = new ConversionContext(doc);
        ctx.BuildLookups();

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            OmitXmlDeclaration = true,
            NewLineOnAttributes = false
        };

        using var writer = XmlWriter.Create(sb, settings);
        ctx.WriteXaml(writer);
        writer.Flush();

        return sb.ToString();
    }
}

internal class ConversionContext
{
    private readonly BamlDocument _doc;
    private readonly Dictionary<ushort, AssemblyInfoRecord> _assemblies = new();
    private readonly Dictionary<ushort, TypeInfoRecord> _types = new();
    private readonly Dictionary<ushort, AttributeInfoRecord> _attributes = new();
    private readonly Dictionary<ushort, StringInfoRecord> _strings = new();
    private readonly Dictionary<string, string> _xmlnsPrefixes = new();
    private readonly Dictionary<string, string> _xmlnsUris = new();
    private readonly List<XmlnsPropertyRecord> _xmlnsRecords = new();
    private int _recordIndex;

    public ConversionContext(BamlDocument doc)
    {
        _doc = doc;
    }

    public void BuildLookups()
    {
        foreach (var record in _doc)
        {
            switch (record)
            {
                case AssemblyInfoRecord air:
                    _assemblies[air.AssemblyId] = air;
                    break;
                case TypeInfoRecord tir:
                    _types[tir.TypeId] = tir;
                    break;
                case AttributeInfoRecord attr:
                    _attributes[attr.AttributeId] = attr;
                    break;
                case StringInfoRecord str:
                    _strings[str.StringId] = str;
                    break;
                case XmlnsPropertyRecord xmlns:
                    _xmlnsRecords.Add(xmlns);
                    break;
            }
        }
    }

    public void WriteXaml(XmlWriter writer)
    {
        _recordIndex = 0;
        var records = _doc.ToList();

        while (_recordIndex < records.Count)
        {
            var record = records[_recordIndex];
            ProcessRecord(writer, record, records);
            _recordIndex++;
        }
    }

    private void ProcessRecord(XmlWriter writer, BamlRecord record, List<BamlRecord> records)
    {
        switch (record)
        {
            case DocumentStartRecord:
                // Skip - just marks document start
                break;

            case DocumentEndRecord:
                // Skip - just marks document end
                break;

            case ElementStartRecord esr:
                WriteElementStart(writer, esr);
                break;

            case ElementEndRecord:
                writer.WriteEndElement();
                break;

            case NamedElementStartRecord nesr:
                WriteNamedElementStart(writer, nesr);
                break;

            case XmlnsPropertyRecord xmlns:
                // Already collected in BuildLookups, write as attribute on current element
                break;

            case PropertyRecord pr:
                WriteProperty(writer, pr);
                break;

            case PropertyWithConverterRecord pwcr:
                WritePropertyWithConverter(writer, pwcr);
                break;

            case PropertyCustomRecord pcr:
                WritePropertyCustom(writer, pcr);
                break;

            case PropertyComplexStartRecord pcsr:
                WritePropertyComplexStart(writer, pcsr);
                break;

            case PropertyComplexEndRecord:
                writer.WriteEndElement();
                break;

            case PropertyListStartRecord plsr:
                WritePropertyListStart(writer, plsr);
                break;

            case PropertyListEndRecord:
                writer.WriteEndElement();
                break;

            case PropertyDictionaryStartRecord pdsr:
                WritePropertyDictionaryStart(writer, pdsr);
                break;

            case PropertyDictionaryEndRecord:
                writer.WriteEndElement();
                break;

            case PropertyArrayStartRecord pasr:
                WritePropertyArrayStart(writer, pasr);
                break;

            case PropertyArrayEndRecord:
                writer.WriteEndElement();
                break;

            case PropertyWithExtensionRecord pwer:
                WritePropertyWithExtension(writer, pwer);
                break;

            case PropertyTypeReferenceRecord ptrr:
                WritePropertyTypeReference(writer, ptrr);
                break;

            case PropertyWithStaticResourceIdRecord:
                // Complex - skip for now
                break;

            case ContentPropertyRecord:
                // Indicates the content property - no XML output needed
                break;

            case DefAttributeRecord dar:
                WriteDefAttribute(writer, dar);
                break;

            case DefAttributeKeyStringRecord daksr:
                WriteDefAttributeKeyString(writer, daksr);
                break;

            case DefAttributeKeyTypeRecord daktr:
                WriteDefAttributeKeyType(writer, daktr);
                break;

            case TextRecord tr:
                writer.WriteString(tr.Value);
                break;

            case TextWithConverterRecord twcr:
                writer.WriteString(twcr.Value);
                break;

            case TextWithIdRecord twir:
                WriteTextWithId(writer, twir);
                break;

            case LiteralContentRecord lcr:
                writer.WriteRaw(lcr.Value);
                break;

            case ConnectionIdRecord:
                // Used for event wiring - skip
                break;

            case DeferableContentStartRecord:
                // Marks start of deferred content
                break;

            case StaticResourceStartRecord srsr:
                WriteStaticResourceStart(writer, srsr);
                break;

            case StaticResourceEndRecord:
                writer.WriteEndElement();
                break;

            case StaticResourceIdRecord:
                // Reference to a static resource
                break;

            case OptimizedStaticResourceRecord osr:
                WriteOptimizedStaticResource(writer, osr);
                break;

            case KeyElementStartRecord kesr:
                WriteKeyElementStart(writer, kesr);
                break;

            case KeyElementEndRecord:
                // End of key element
                break;

            case ConstructorParametersStartRecord:
                // Start of constructor params
                break;

            case ConstructorParametersEndRecord:
                // End of constructor params
                break;

            case ConstructorParameterTypeRecord cptr:
                WriteConstructorParameterType(writer, cptr);
                break;

            case PIMappingRecord:
                // PI mapping - namespace mapping
                break;

            case PresentationOptionsAttributeRecord poar:
                WritePresentationOptionsAttribute(writer, poar);
                break;

            case RoutedEventRecord rer:
                WriteRoutedEvent(writer, rer);
                break;

            case LineNumberAndPositionRecord:
            case LinePositionRecord:
                // Debug info - skip
                break;

            default:
                // Unknown record type - skip
                break;
            }
    }

    private void WriteElementStart(XmlWriter writer, ElementStartRecord esr)
    {
        var (ns, localName) = ResolveType(esr.TypeId);
        var prefix = GetPrefixForNamespace(ns);

        if (string.IsNullOrEmpty(prefix))
            writer.WriteStartElement(localName, ns);
        else
            writer.WriteStartElement(prefix, localName, ns);

        // Write xmlns declarations that haven't been written yet
        WriteXmlnsDeclarations(writer);
    }

    private void WriteNamedElementStart(XmlWriter writer, NamedElementStartRecord nesr)
    {
        var (ns, localName) = ResolveType(nesr.TypeId);
        var prefix = GetPrefixForNamespace(ns);

        if (string.IsNullOrEmpty(prefix))
            writer.WriteStartElement(localName, ns);
        else
            writer.WriteStartElement(prefix, localName, ns);

        // Write x:Name attribute
        if (!string.IsNullOrEmpty(nesr.RuntimeName))
        {
            writer.WriteAttributeString("x", "Name", "http://schemas.microsoft.com/winfx/2006/xaml", nesr.RuntimeName);
        }

        WriteXmlnsDeclarations(writer);
    }

    private bool _xmlnsWritten = false;

    private void WriteXmlnsDeclarations(XmlWriter writer)
    {
        if (_xmlnsWritten) return;
        _xmlnsWritten = true;

        foreach (var xmlns in _xmlnsRecords)
        {
            var uri = xmlns.XmlNamespace;
            var prefix = xmlns.Prefix;

            if (string.IsNullOrEmpty(prefix))
            {
                // Default namespace - already handled by WriteStartElement
            }
            else
            {
                try
                {
                    writer.WriteAttributeString("xmlns", prefix, null, uri);
                }
                catch
                {
                    // Namespace already declared
                }
            }

            _xmlnsPrefixes[uri] = prefix;
        }
    }

    private void WriteProperty(XmlWriter writer, PropertyRecord pr)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(pr.AttributeId);
        WriteAttributeValue(writer, attrNs, attrName, ownerType, pr.Value);
    }

    private void WritePropertyWithConverter(XmlWriter writer, PropertyWithConverterRecord pwcr)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(pwcr.AttributeId);
        WriteAttributeValue(writer, attrNs, attrName, ownerType, pwcr.Value);
    }

    private void WritePropertyCustom(XmlWriter writer, PropertyCustomRecord pcr)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(pcr.AttributeId);
        // PropertyCustom has binary data - try to interpret it
        var value = InterpretPropertyCustom(pcr);
        WriteAttributeValue(writer, attrNs, attrName, ownerType, value);
    }

    private void WritePropertyComplexStart(XmlWriter writer, PropertyComplexStartRecord pcsr)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(pcsr.AttributeId);
        var elementName = $"{ownerType}.{attrName}";
        var prefix = GetPrefixForNamespace(attrNs);

        if (string.IsNullOrEmpty(prefix))
            writer.WriteStartElement(elementName, attrNs);
        else
            writer.WriteStartElement(prefix, elementName, attrNs);
    }

    private void WritePropertyListStart(XmlWriter writer, PropertyListStartRecord plsr)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(plsr.AttributeId);
        var elementName = $"{ownerType}.{attrName}";
        var prefix = GetPrefixForNamespace(attrNs);

        if (string.IsNullOrEmpty(prefix))
            writer.WriteStartElement(elementName, attrNs);
        else
            writer.WriteStartElement(prefix, elementName, attrNs);
    }

    private void WritePropertyDictionaryStart(XmlWriter writer, PropertyDictionaryStartRecord pdsr)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(pdsr.AttributeId);
        var elementName = $"{ownerType}.{attrName}";
        var prefix = GetPrefixForNamespace(attrNs);

        if (string.IsNullOrEmpty(prefix))
            writer.WriteStartElement(elementName, attrNs);
        else
            writer.WriteStartElement(prefix, elementName, attrNs);
    }

    private void WritePropertyArrayStart(XmlWriter writer, PropertyArrayStartRecord pasr)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(pasr.AttributeId);
        var elementName = $"{ownerType}.{attrName}";
        var prefix = GetPrefixForNamespace(attrNs);

        if (string.IsNullOrEmpty(prefix))
            writer.WriteStartElement(elementName, attrNs);
        else
            writer.WriteStartElement(prefix, elementName, attrNs);
    }

    private void WritePropertyWithExtension(XmlWriter writer, PropertyWithExtensionRecord pwer)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(pwer.AttributeId);
        
        // The extension value encodes a markup extension reference
        var valueId = pwer.ValueId;
        var flags = pwer.Flags;
        string value;

        // Flags indicate the type of extension
        var extensionTypeId = (short)(flags & 0xFFF);
        bool isValueType = (flags & 0x4000) != 0;
        bool isStaticType = (flags & 0x2000) != 0;

        if (isValueType && _types.ContainsKey((ushort)valueId))
        {
            var (_, typeName) = ResolveType((ushort)valueId);
            value = $"{{x:Type {typeName}}}";
        }
        else if (isStaticType && _attributes.ContainsKey((ushort)valueId))
        {
            var (_, memberName, memberOwner) = ResolveAttribute((ushort)valueId);
            value = $"{{x:Static {memberOwner}.{memberName}}}";
        }
        else
        {
            // Try to resolve as a resource reference
            var extTypeName = ResolveExtensionType(extensionTypeId);
            if (_strings.ContainsKey((ushort)valueId))
            {
                value = $"{{{extTypeName} {_strings[(ushort)valueId].Value}}}";
            }
            else if (_attributes.ContainsKey((ushort)valueId))
            {
                var (_, memberName, memberOwner) = ResolveAttribute((ushort)valueId);
                value = $"{{{extTypeName} {memberOwner}.{memberName}}}";
            }
            else
            {
                value = $"{{{extTypeName} #{valueId}}}";
            }
        }

        WriteAttributeValue(writer, attrNs, attrName, ownerType, value);
    }

    private void WritePropertyTypeReference(XmlWriter writer, PropertyTypeReferenceRecord ptrr)
    {
        var (attrNs, attrName, ownerType) = ResolveAttribute(ptrr.AttributeId);
        var (_, typeName) = ResolveType(ptrr.TypeId);
        WriteAttributeValue(writer, attrNs, attrName, ownerType, $"{{x:Type {typeName}}}");
    }

    private void WriteDefAttribute(XmlWriter writer, DefAttributeRecord dar)
    {
        var name = dar.Name;
        var value = dar.Value;
        writer.WriteAttributeString("x", name, "http://schemas.microsoft.com/winfx/2006/xaml", value);
    }

    private void WriteDefAttributeKeyString(XmlWriter writer, DefAttributeKeyStringRecord daksr)
    {
        var value = ResolveString(daksr.ValueId);
        writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", value);
    }

    private void WriteDefAttributeKeyType(XmlWriter writer, DefAttributeKeyTypeRecord daktr)
    {
        var (_, typeName) = ResolveType(daktr.TypeId);
        writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", $"{{x:Type {typeName}}}");
    }

    private void WriteTextWithId(XmlWriter writer, TextWithIdRecord twir)
    {
        var value = ResolveString(twir.ValueId);
        writer.WriteString(value);
    }

    private void WriteStaticResourceStart(XmlWriter writer, StaticResourceStartRecord srsr)
    {
        var (ns, localName) = ResolveType(srsr.TypeId);
        var prefix = GetPrefixForNamespace(ns);

        if (string.IsNullOrEmpty(prefix))
            writer.WriteStartElement(localName, ns);
        else
            writer.WriteStartElement(prefix, localName, ns);
    }

    private void WriteOptimizedStaticResource(XmlWriter writer, OptimizedStaticResourceRecord osr)
    {
        // This represents an optimized static resource reference
        // The flags and valueId encode the reference
    }

    private void WriteKeyElementStart(XmlWriter writer, KeyElementStartRecord kesr)
    {
        // Key element - write x:Key
        var (_, typeName) = ResolveType(kesr.TypeId);
        writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", $"{{x:Type {typeName}}}");
    }

    private void WriteConstructorParameterType(XmlWriter writer, ConstructorParameterTypeRecord cptr)
    {
        var (_, typeName) = ResolveType(cptr.TypeId);
        writer.WriteString(typeName);
    }

    private void WritePresentationOptionsAttribute(XmlWriter writer, PresentationOptionsAttributeRecord poar)
    {
        writer.WriteAttributeString("PresentationOptions", poar.Name,
            "http://schemas.microsoft.com/winfx/2006/xaml/presentation/options", poar.Value);
    }

    private void WriteRoutedEvent(XmlWriter writer, RoutedEventRecord rer)
    {
        var (_, attrName, _) = ResolveAttribute(rer.AttributeId);
        writer.WriteAttributeString(attrName, rer.Value);
    }

    private void WriteAttributeValue(XmlWriter writer, string ns, string attrName, string ownerType, string value)
    {
        // For attributes in the default namespace, just write the local name
        // For attached properties, write OwnerType.PropertyName
        if (IsAttachedProperty(attrName, ownerType))
        {
            var prefix = GetPrefixForNamespace(ns);
            if (string.IsNullOrEmpty(prefix))
                writer.WriteAttributeString($"{ownerType}.{attrName}", value);
            else
                writer.WriteAttributeString(prefix, $"{ownerType}.{attrName}", ns, value);
        }
        else
        {
            writer.WriteAttributeString(attrName, value);
        }
    }

    private bool IsAttachedProperty(string attrName, string ownerType)
    {
        // Heuristic: if the attribute's owner type doesn't match the current element,
        // it's likely an attached property. For simplicity, we check common patterns.
        // A more accurate approach would track the current element type.
        return !string.IsNullOrEmpty(ownerType) && 
               (attrName == "Row" || attrName == "Column" || attrName == "RowSpan" || 
                attrName == "ColumnSpan" || attrName == "DockPanel" || attrName == "Dock" ||
                attrName == "Left" || attrName == "Top" || attrName == "Right" || attrName == "Bottom" ||
                attrName == "ZIndex" || attrName == "IsSharedSizeScope" || attrName == "SharedSizeGroup");
    }

    private (string Namespace, string LocalName) ResolveType(ushort typeId)
    {
        if (_types.TryGetValue(typeId, out var typeInfo))
        {
            var typeName = typeInfo.TypeFullName;
            
            // Strip the namespace prefix if it's a CLR type name
            var lastDot = typeName.LastIndexOf('.');
            string localName = lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
            string clrNamespace = lastDot >= 0 ? typeName.Substring(0, lastDot) : "";

            // Find the xmlns URI for this CLR namespace
            var ns = FindXmlnsForClrNamespace(clrNamespace, typeInfo.AssemblyId);
            return (ns, localName);
        }

        // Known type by negative ID
        if (KnownTypeTable.TryGetValue(typeId, out var known))
        {
            return ("http://schemas.microsoft.com/winfx/2006/xaml/presentation", known);
        }

        return ("http://schemas.microsoft.com/winfx/2006/xaml/presentation", $"UnknownType_{typeId}");
    }

    private (string Namespace, string Name, string OwnerType) ResolveAttribute(ushort attributeId)
    {
        if (_attributes.TryGetValue(attributeId, out var attrInfo))
        {
            var name = attrInfo.Name;
            var (ownerNs, ownerName) = ResolveType(attrInfo.OwnerTypeId);
            return (ownerNs, name, ownerName);
        }

        // Known attribute by negative ID
        if (KnownAttributeTable.TryGetValue(attributeId, out var knownAttr))
        {
            return ("http://schemas.microsoft.com/winfx/2006/xaml/presentation", knownAttr.Name, knownAttr.OwnerType);
        }

        return ("", $"UnknownAttr_{attributeId}", "");
    }

    private string ResolveString(ushort stringId)
    {
        if (_strings.TryGetValue(stringId, out var str))
            return str.Value;
        return $"#String_{stringId}";
    }

    private string FindXmlnsForClrNamespace(string clrNamespace, ushort assemblyId)
    {
        // Check xmlns records for a matching clr-namespace mapping
        foreach (var xmlns in _xmlnsRecords)
        {
            var uri = xmlns.XmlNamespace;
            if (uri.StartsWith("clr-namespace:", StringComparison.Ordinal))
            {
                var parts = uri.Split(';');
                var ns = parts[0].Substring("clr-namespace:".Length);
                if (ns == clrNamespace)
                    return uri;
            }
            else if (IsWpfNamespace(uri) && IsWpfClrNamespace(clrNamespace))
            {
                return uri;
            }
        }

        // Default to presentation namespace for known WPF types
        if (IsWpfClrNamespace(clrNamespace))
            return "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        // Build a clr-namespace URI
        var asmName = _assemblies.TryGetValue(assemblyId, out var asm) ? asm.AssemblyFullName.Split(',')[0] : "";
        return $"clr-namespace:{clrNamespace};assembly={asmName}";
    }

    private string GetPrefixForNamespace(string ns)
    {
        if (_xmlnsPrefixes.TryGetValue(ns, out var prefix))
            return prefix;

        // Default namespace
        if (ns == "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            return "";
        if (ns == "http://schemas.microsoft.com/winfx/2006/xaml")
            return "x";

        return "";
    }

    private static bool IsWpfNamespace(string uri)
    {
        return uri == "http://schemas.microsoft.com/winfx/2006/xaml/presentation" ||
               uri == "http://schemas.microsoft.com/winfx/2006/xaml";
    }

    private static bool IsWpfClrNamespace(string clrNamespace)
    {
        return clrNamespace.StartsWith("System.Windows", StringComparison.Ordinal) ||
               clrNamespace == "System.Windows" ||
               clrNamespace.StartsWith("System.Windows.", StringComparison.Ordinal);
    }

    private string InterpretPropertyCustom(PropertyCustomRecord pcr)
    {
        // PropertyCustom contains binary-encoded values
        // The SerializerTypeId indicates how to interpret the data
        var data = pcr.Data;
        if (data == null || data.Length == 0)
            return "";

        try
        {
            // Common cases: Bool, Brush (SolidColorBrush from color)
            var serializerTypeId = pcr.SerializerTypeId;
            
            // Type 0x89 = Boolean
            if (serializerTypeId == 0x89 || serializerTypeId == 46)
            {
                return data[0] != 0 ? "True" : "False";
            }

            // Type 0x2E = SolidColorBrush / Color
            if (serializerTypeId == 0xC5 || serializerTypeId == 197)
            {
                if (data.Length >= 4)
                {
                    // ARGB color
                    var a = data[0];
                    var r = data[1];
                    var g = data[2];
                    var b = data[3];
                    if (a == 255)
                        return $"#{r:X2}{g:X2}{b:X2}";
                    return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
                }
            }

            // Try to interpret as a type reference
            if ((serializerTypeId & 0x4000) != 0)
            {
                // Type reference
                if (data.Length >= 2)
                {
                    var typeId = BitConverter.ToUInt16(data, 0);
                    var (_, typeName) = ResolveType(typeId);
                    return typeName;
                }
            }

            // Fallback: try as string
            return Convert.ToBase64String(data);
        }
        catch
        {
            return Convert.ToBase64String(data ?? Array.Empty<byte>());
        }
    }

    private string ResolveExtensionType(short extensionTypeId)
    {
        return extensionTypeId switch
        {
            0x25E or 602 => "StaticResource",
            0x25F or 603 => "DynamicResource",
            0x27A or 634 => "Binding",
            0x260 or 608 => "TemplateBinding",
            0x261 or 609 => "x:Static",
            0x262 or 610 => "x:Type",
            0x263 or 611 => "x:Null",
            _ => $"Extension_{extensionTypeId}"
        };
    }

    // Known WPF type table (subset - negative type IDs map to built-in types)
    private static readonly Dictionary<ushort, string> KnownTypeTable = BuildKnownTypeTable();
    private static readonly Dictionary<ushort, (string Name, string OwnerType)> KnownAttributeTable = BuildKnownAttributeTable();

    private static Dictionary<ushort, string> BuildKnownTypeTable()
    {
        // This is a subset of the known types. Full table has ~800 entries.
        var table = new Dictionary<ushort, string>();
        // Negative IDs are stored as ushort (two's complement)
        return table;
    }

    private static Dictionary<ushort, (string Name, string OwnerType)> BuildKnownAttributeTable()
    {
        var table = new Dictionary<ushort, (string, string)>();
        return table;
    }
}

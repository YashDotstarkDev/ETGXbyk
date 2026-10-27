using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Xml;

namespace ETG.Admin.Features.EnquiryManagement.Enquires;

// Minimal, real Open XML workbook; no additional NuGet dependency.
internal static class EnquiryExcelExporter
{
    private const string SheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly string[] Headers =
    {
        "ID", "Date created", "Destination", "Status", "Consultant", "First name",
        "Last name", "Email", "Phone", "Preferred contact method", "Experience",
        "Tour name", "Tour code", "UTM campaign", "UTM content", "UTM medium",
        "UTM source", "UTM term", "Last updated date"
    };

    internal static byte[] Build(IEnumerable<EnquiryDto> rows, CancellationToken token)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WritePart(zip, "[Content_Types].xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                </Types>
                """);
            WritePart(zip, "_rels/.rels", """
                <?xml version="1.0" encoding="utf-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            WritePart(zip, "xl/workbook.xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Enquiries" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            WritePart(zip, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="utf-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);
            WritePart(zip, "xl/styles.xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
                  <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
                  <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
                  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                  <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>
                  <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
                </styleSheet>
                """);
            using var stream = zip.CreateEntry("xl/worksheets/sheet1.xml").Open();
            using var xml = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
            xml.WriteStartDocument();
            xml.WriteStartElement("worksheet", SheetNamespace);
            xml.WriteStartElement("sheetViews", SheetNamespace);
            xml.WriteStartElement("sheetView", SheetNamespace);
            xml.WriteAttributeString("workbookViewId", "0");
            xml.WriteStartElement("pane", SheetNamespace);
            xml.WriteAttributeString("ySplit", "1");
            xml.WriteAttributeString("topLeftCell", "A2");
            xml.WriteAttributeString("activePane", "bottomLeft");
            xml.WriteAttributeString("state", "frozen");
            xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndElement();
            xml.WriteStartElement("cols", SheetNamespace);
            for (int i = 0; i < Headers.Length; i++)
            {
                xml.WriteStartElement("col", SheetNamespace);
                xml.WriteAttributeString("min", (i + 1).ToString(CultureInfo.InvariantCulture));
                xml.WriteAttributeString("max", (i + 1).ToString(CultureInfo.InvariantCulture));
                xml.WriteAttributeString("width", i == 0 ? "10" : i == 7 ? "35" : "25");
                xml.WriteAttributeString("customWidth", "1");
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            xml.WriteStartElement("sheetData", SheetNamespace);
            WriteRow(xml, 1, Headers, header: true);
            int rowNumber = 1;
            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                if (++rowNumber > 1048576)
                    throw new InvalidOperationException("The Excel worksheet row limit was exceeded.");
                WriteRow(xml, rowNumber, new[]
                {
                    row.ItemId.ToString(CultureInfo.InvariantCulture), row.FormInserted,
                    row.PreferredDestination, row.Status, row.ConsultantName,
                    row.FirstName, row.LastName, row.Email, row.Phone,
                    row.PreferredContactMethod, row.PreferredExperience, row.TourName,
                    row.TourCode, row.UTMCampaign, row.UTMContent, row.UTMMedium,
                    row.UTMSource, row.UTMTerm, row.FormUpdated
                }, header: false);
            }
            xml.WriteEndElement();
            xml.WriteStartElement("autoFilter", SheetNamespace);
            xml.WriteAttributeString("ref", $"A1:S{rowNumber}");
            xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndDocument();
        }
        return output.ToArray();
    }

    private static void WriteRow(XmlWriter xml, int index, string[] values, bool header)
    {
        xml.WriteStartElement("row", SheetNamespace);
        xml.WriteAttributeString("r", index.ToString(CultureInfo.InvariantCulture));
        for (int i = 0; i < values.Length; i++)
        {
            xml.WriteStartElement("c", SheetNamespace);
            xml.WriteAttributeString("r", $"{(char)('A' + i)}{index}");
            if (header) xml.WriteAttributeString("s", "1");
            if (i == 0 && !header)
            {
                xml.WriteElementString("v", SheetNamespace, values[i]);
            }
            else
            {
                // Text cells preserve phone leading zeros and never execute formulas.
                xml.WriteAttributeString("t", "inlineStr");
                xml.WriteStartElement("is", SheetNamespace);
                xml.WriteStartElement("t", SheetNamespace);
                xml.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                xml.WriteString(CleanXml(values[i] ?? string.Empty));
                xml.WriteEndElement(); xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    private static string CleanXml(string text)
    {
        var result = new StringBuilder();
        foreach (var rune in text.EnumerateRunes())
        {
            int value = rune.Value;
            if (value == 9 || value == 10 || value == 13 ||
                value >= 0x20 && value <= 0xD7FF ||
                value >= 0xE000 && value <= 0xFFFD ||
                value >= 0x10000 && value <= 0x10FFFF)
                result.Append(rune.ToString());
        }
        string cleaned = result.ToString();
        if (cleaned.Length > 32767)
            throw new InvalidOperationException("An export cell exceeds Excel's text limit.");
        return cleaned;
    }

    private static void WritePart(ZipArchive zip, string path, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}

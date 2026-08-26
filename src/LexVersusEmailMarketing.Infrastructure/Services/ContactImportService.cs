using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using LexVersusEmailMarketing.Core.DTOs;
using LexVersusEmailMarketing.Core.Entities;
using LexVersusEmailMarketing.Core.Interfaces;
using LexVersusEmailMarketing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LexVersusEmailMarketing.Infrastructure.Services;

public partial class ContactImportService(AppDbContext db) : IContactImportService
{
    private static readonly Regex EmailRegex = MyRegex();

    public async Task<ImportResultDto> ImportFromFileAsync(
        Stream fileStream,
        string fileName,
        Guid? emailListId,
        CancellationToken cancellationToken = default)
    {
        var rows = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)
            ? ParseExcel(fileStream)
            : ParseCsv(fileStream);

        var errors = new List<string>();
        var imported = 0;
        var skipped = 0;
        var duplicates = 0;

        EmailList? emailList = null;
        if (emailListId.HasValue)
        {
            emailList = await db.EmailLists.FindAsync([emailListId.Value], cancellationToken);
            if (emailList is null)
            {
                return new ImportResultDto(0, 0, 0, ["Email list not found."]);
            }
        }

        var existingEmails = await db.Contacts
            .Select(c => c.Email.ToLower())
            .ToHashSetAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Email))
            {
                skipped++;
                continue;
            }

            var email = row.Email.Trim().ToLowerInvariant();
            if (!EmailRegex.IsMatch(email))
            {
                errors.Add($"Invalid email: {row.Email}");
                skipped++;
                continue;
            }

            Contact contact;
            if (existingEmails.Contains(email))
            {
                contact = await db.Contacts.FirstAsync(c => c.Email == email, cancellationToken);
                duplicates++;
            }
            else
            {
                contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FirstName = row.FirstName?.Trim(),
                    LastName = row.LastName?.Trim()
                };
                db.Contacts.Add(contact);
                existingEmails.Add(email);
                imported++;
            }

            if (emailList is not null)
            {
                var alreadyInList = await db.EmailListContacts
                    .AnyAsync(elc => elc.EmailListId == emailList.Id && elc.ContactId == contact.Id, cancellationToken);

                if (!alreadyInList)
                {
                    db.EmailListContacts.Add(new EmailListContact
                    {
                        EmailListId = emailList.Id,
                        ContactId = contact.Id
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ImportResultDto(imported, skipped, duplicates, errors);
    }

    private static List<ImportRow> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant().Trim()
        };

        using var csv = new CsvReader(reader, config);
        var records = new List<ImportRow>();

        if (!csv.Read() || !csv.ReadHeader())
        {
            return records;
        }

        while (csv.Read())
        {
            records.Add(new ImportRow
            {
                Email = GetField(csv, "email", "e-mail", "mail"),
                FirstName = GetField(csv, "firstname", "first_name", "first name", "ime"),
                LastName = GetField(csv, "lastname", "last_name", "last name", "prezime")
            });
        }

        return records;
    }

    private static string? GetField(CsvReader csv, params string[] names)
    {
        foreach (var name in names)
        {
            try
            {
                var value = csv.GetField(name);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch
            {
                // Column not found, try next alias
            }
        }

        return null;
    }

    private static List<ImportRow> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var rows = new List<ImportRow>();

        var headerRow = worksheet.FirstRowUsed();
        if (headerRow is null)
        {
            return rows;
        }

        var headers = headerRow.CellsUsed()
            .ToDictionary(c => c.Address.ColumnNumber, c => c.GetString().Trim().ToLowerInvariant());

        var emailCol = FindColumn(headers, "email", "e-mail", "mail");
        if (emailCol is null)
        {
            return rows;
        }

        var firstNameCol = FindColumn(headers, "firstname", "first_name", "first name", "ime");
        var lastNameCol = FindColumn(headers, "lastname", "last_name", "last name", "prezime");

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            rows.Add(new ImportRow
            {
                Email = row.Cell(emailCol.Value).GetString(),
                FirstName = firstNameCol.HasValue ? row.Cell(firstNameCol.Value).GetString() : null,
                LastName = lastNameCol.HasValue ? row.Cell(lastNameCol.Value).GetString() : null
            });
        }

        return rows;
    }

    private static int? FindColumn(Dictionary<int, string> headers, params string[] names)
    {
        foreach (var (col, header) in headers)
        {
            if (names.Any(n => header.Contains(n, StringComparison.OrdinalIgnoreCase)))
            {
                return col;
            }
        }

        return null;
    }

    private sealed class ImportRow
    {
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex MyRegex();
}

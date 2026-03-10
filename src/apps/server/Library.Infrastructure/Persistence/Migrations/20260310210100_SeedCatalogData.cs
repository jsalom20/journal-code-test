using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Infrastructure.Persistence.Migrations
{
    public partial class SeedCatalogData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var records = GetSeedRecords();
            var authorNames = records
                .SelectMany(record => record.Authors)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            foreach (var authorName in authorNames)
            {
                migrationBuilder.InsertData(
                    table: "Authors",
                    columns: new[] { "Id", "Name" },
                    values: new object[] { GetAuthorId(authorName), authorName });
            }

            foreach (var record in records)
            {
                migrationBuilder.InsertData(
                    table: "Books",
                    columns: new[]
                    {
                        "Id",
                        "Title",
                        "Isbn13",
                        "Language",
                        "Publisher",
                        "Summary",
                        "PublicationYear",
                        "CreatedAtUtc",
                        "UpdatedAtUtc"
                    },
                    values: new object[]
                    {
                        GetBookId(record.Id),
                        record.Title,
                        record.Isbn13,
                        record.Language,
                        record.Publisher,
                        record.Summary,
                        record.PublicationYear,
                        record.CreatedAtUtc,
                        record.UpdatedAtUtc
                    });

                foreach (var authorName in record.Authors.Distinct(StringComparer.Ordinal))
                {
                    migrationBuilder.InsertData(
                        table: "BookAuthors",
                        columns: new[] { "BookId", "AuthorId" },
                        values: new object[] { GetBookId(record.Id), GetAuthorId(authorName) });
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var records = GetSeedRecords();
            var bookIds = records.Select(record => GetBookId(record.Id)).ToList();
            var authorIds = records
                .SelectMany(record => record.Authors)
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(name => name, StringComparer.Ordinal)
                .Select(GetAuthorId)
                .ToList();

            if (bookIds.Count > 0)
            {
                var bookIdList = string.Join(", ", bookIds.Select(id => $"'{id}'"));
                migrationBuilder.Sql($"DELETE FROM \"CopyEvents\" WHERE \"CopyId\" IN (SELECT \"Id\" FROM \"BookCopies\" WHERE \"BookId\" IN ({bookIdList}));");
                migrationBuilder.Sql($"DELETE FROM \"Loans\" WHERE \"CopyId\" IN (SELECT \"Id\" FROM \"BookCopies\" WHERE \"BookId\" IN ({bookIdList}));");
                migrationBuilder.Sql($"DELETE FROM \"Reservations\" WHERE \"BookId\" IN ({bookIdList});");
                migrationBuilder.Sql($"DELETE FROM \"BookCopies\" WHERE \"BookId\" IN ({bookIdList});");
            }

            foreach (var record in records)
            {
                foreach (var authorName in record.Authors.Distinct(StringComparer.Ordinal))
                {
                    migrationBuilder.DeleteData(
                        table: "BookAuthors",
                        keyColumns: new[] { "BookId", "AuthorId" },
                        keyValues: new object[] { GetBookId(record.Id), GetAuthorId(authorName) });
                }
            }

            foreach (var record in records)
            {
                migrationBuilder.DeleteData(
                    table: "Books",
                    keyColumn: "Id",
                    keyValue: GetBookId(record.Id));
            }

            foreach (var authorId in authorIds)
            {
                migrationBuilder.DeleteData(
                    table: "Authors",
                    keyColumn: "Id",
                    keyValue: authorId);
            }
        }

        private static List<CatalogSeedBook> GetSeedRecords()
        {
            return JsonSerializer.Deserialize<List<CatalogSeedBook>>(CatalogSeedJson)
                ?? throw new InvalidOperationException("Catalog seed data could not be deserialized.");
        }

        private static Guid GetBookId(string seedId)
        {
            return CreateDeterministicGuid($"catalog-book:{seedId}");
        }

        private static Guid GetAuthorId(string authorName)
        {
            return CreateDeterministicGuid($"catalog-author:{authorName}");
        }

        private static Guid CreateDeterministicGuid(string value)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
            return new Guid(hash);
        }

        private sealed class CatalogSeedBook
        {
            public string Id { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Isbn13 { get; init; } = string.Empty;
            public string Language { get; init; } = string.Empty;
            public string Publisher { get; init; } = string.Empty;
            public string Summary { get; init; } = string.Empty;
            public int? PublicationYear { get; init; }
            public DateTime CreatedAtUtc { get; init; }
            public DateTime UpdatedAtUtc { get; init; }
            public List<string> Authors { get; init; } = [];
        }

        private const string CatalogSeedJson =
"""
[
  {
    "Id": "1",
    "Title": "Working Small Garden Year",
    "Isbn13": "9780000000019",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2022,
    "CreatedAtUtc": "2025-09-02T23:20:00Z",
    "UpdatedAtUtc": "2026-01-01T18:30:00Z",
    "Authors": [
      "Sofia Sjöström"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "2",
    "Title": "The Paper Dragon",
    "Isbn13": "9780000000026",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2023-11-28T07:25:00Z",
    "UpdatedAtUtc": "2026-01-10T07:37:00Z",
    "Authors": [
      "Daniel Morgan"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "3",
    "Title": "A North Wind",
    "Isbn13": "9780000000033",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2024-04-22T22:20:00Z",
    "UpdatedAtUtc": "2026-01-05T12:08:00Z",
    "Authors": [
      "Saga Hedlund"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "4",
    "Title": "Working Creative Habit Workbook",
    "Isbn13": "9780000000040",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2019,
    "CreatedAtUtc": "2023-09-09T04:59:00Z",
    "UpdatedAtUtc": "2026-02-02T07:23:00Z",
    "Authors": [
      "Astrid Engström",
      "Elin Molin",
      "Maja Ekman"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "5",
    "Title": "The Missing Thaw",
    "Isbn13": "9780000000057",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2020,
    "CreatedAtUtc": "2024-11-04T04:16:00Z",
    "UpdatedAtUtc": "2026-01-02T23:35:00Z",
    "Authors": [
      "Matilda Lindberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "6",
    "Title": "The Last Memory Colony",
    "Isbn13": "9780000000064",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2024-01-03T20:27:00Z",
    "UpdatedAtUtc": "2026-02-01T00:21:00Z",
    "Authors": [
      "Hannah Griffin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "7",
    "Title": "Practical Tiny Balcony Garden",
    "Isbn13": "9780000000071",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2023-02-23T04:34:00Z",
    "UpdatedAtUtc": "2026-01-06T18:35:00Z",
    "Authors": [
      "Jonathan Lawson"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "8",
    "Title": "The Engineer of Light",
    "Isbn13": "9780000000088",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2025-04-22T03:22:00Z",
    "UpdatedAtUtc": "2026-03-07T19:47:00Z",
    "Authors": [
      "Karin Björk"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "9",
    "Title": "Dark Winter File",
    "Isbn13": "9780000000095",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 1995,
    "CreatedAtUtc": "2025-02-13T01:54:00Z",
    "UpdatedAtUtc": "2026-02-04T06:52:00Z",
    "Authors": [
      "Emilia Åkesson",
      "Ebba Lundqvist"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "10",
    "Title": "The Last Fifth Archive",
    "Isbn13": "9780000000101",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2006,
    "CreatedAtUtc": "2024-02-25T08:22:00Z",
    "UpdatedAtUtc": "2026-03-09T12:43:00Z",
    "Authors": [
      "Olivia Ward"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "11",
    "Title": "Chronicles of Skybound Sea",
    "Isbn13": "9780000000118",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 1991,
    "CreatedAtUtc": "2025-07-12T23:50:00Z",
    "UpdatedAtUtc": "2026-02-07T19:32:00Z",
    "Authors": [
      "Alva Sjöström"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "12",
    "Title": "Witness to Summer Republic",
    "Isbn13": "9780000000125",
    "Language": "Danish",
    "Publisher": "Simon & Schuster",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2019,
    "CreatedAtUtc": "2025-12-24T23:42:00Z",
    "UpdatedAtUtc": "2026-01-06T13:04:00Z",
    "Authors": [
      "Anna Griffin"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "13",
    "Title": "Echoes of Frost Signal",
    "Isbn13": "9780000000132",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2024-12-10T17:08:00Z",
    "UpdatedAtUtc": "2026-01-07T21:24:00Z",
    "Authors": [
      "Linnea Hedlund",
      "Linnea Åkesson"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "14",
    "Title": "Everyday Nordic Woodshop",
    "Isbn13": "9780000000149",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2003,
    "CreatedAtUtc": "2023-07-26T18:38:00Z",
    "UpdatedAtUtc": "2026-03-06T14:28:00Z",
    "Authors": [
      "Anna Hayes"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "15",
    "Title": "Working Tiny Balcony Garden",
    "Isbn13": "9780000000156",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2017,
    "CreatedAtUtc": "2025-11-20T10:05:00Z",
    "UpdatedAtUtc": "2026-01-05T07:51:00Z",
    "Authors": [
      "Emily Hayes"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "16",
    "Title": "The Night Caller",
    "Isbn13": "9780000000163",
    "Language": "Danish",
    "Publisher": "Norstedts",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2023-03-21T22:00:00Z",
    "UpdatedAtUtc": "2026-01-07T07:11:00Z",
    "Authors": [
      "Lovisa Åkesson",
      "Anna Norberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "17",
    "Title": "The Secret of Fox in the Library",
    "Isbn13": "9780000000170",
    "Language": "Norwegian",
    "Publisher": "Penguin Books",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2024-08-20T23:57:00Z",
    "UpdatedAtUtc": "2026-03-07T17:28:00Z",
    "Authors": [
      "Amelia Lawson"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "18",
    "Title": "Working Family Meal Planner",
    "Isbn13": "9780000000187",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2024-11-08T08:28:00Z",
    "UpdatedAtUtc": "2026-01-05T07:17:00Z",
    "Authors": [
      "Ebba Lundqvist"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "19",
    "Title": "Chronicles of Iron Moon",
    "Isbn13": "9780000000194",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2023-07-14T10:34:00Z",
    "UpdatedAtUtc": "2026-02-07T01:13:00Z",
    "Authors": [
      "Anna Bergqvist"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "20",
    "Title": "Witness to Northern Reform",
    "Isbn13": "9780000000200",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 2004,
    "CreatedAtUtc": "2024-07-18T23:47:00Z",
    "UpdatedAtUtc": "2026-03-10T07:31:00Z",
    "Authors": [
      "Anna Norberg",
      "Astrid Björk"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "21",
    "Title": "The Starforge",
    "Isbn13": "9780000000217",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2014,
    "CreatedAtUtc": "2023-10-18T00:58:00Z",
    "UpdatedAtUtc": "2026-02-10T18:42:00Z",
    "Authors": [
      "Clara Turner"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "22",
    "Title": "After Birches",
    "Isbn13": "9780000000224",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 2014,
    "CreatedAtUtc": "2024-06-25T12:17:00Z",
    "UpdatedAtUtc": "2026-02-05T02:30:00Z",
    "Authors": [
      "Alva Wallin",
      "Emilia Sandberg"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "23",
    "Title": "The Complete Digital Declutter Plan",
    "Isbn13": "9780000000231",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2023-01-20T04:15:00Z",
    "UpdatedAtUtc": "2026-01-08T21:07:00Z",
    "Authors": [
      "James Bennett"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "24",
    "Title": "Silent Dead Letter",
    "Isbn13": "9780000000248",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 1995,
    "CreatedAtUtc": "2024-02-19T00:59:00Z",
    "UpdatedAtUtc": "2026-02-10T21:58:00Z",
    "Authors": [
      "Nathan Ellis"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "25",
    "Title": "A Cartographer's Daughter",
    "Isbn13": "9780000000255",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2004,
    "CreatedAtUtc": "2025-10-26T03:50:00Z",
    "UpdatedAtUtc": "2026-03-01T11:34:00Z",
    "Authors": [
      "Ebba Holm"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "26",
    "Title": "A Better Sleep Manual",
    "Isbn13": "9780000000262",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2023-07-12T20:57:00Z",
    "UpdatedAtUtc": "2026-02-03T13:11:00Z",
    "Authors": [
      "Astrid Åkesson"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "27",
    "Title": "Adventures of Moonlight Camp",
    "Isbn13": "9780000000279",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2023-02-09T14:15:00Z",
    "UpdatedAtUtc": "2026-02-10T19:42:00Z",
    "Authors": [
      "Eleanor Harper"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "28",
    "Title": "The Ash Kingdom",
    "Isbn13": "9780000000286",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2001,
    "CreatedAtUtc": "2024-05-20T22:56:00Z",
    "UpdatedAtUtc": "2026-02-09T00:33:00Z",
    "Authors": [
      "Olivia Brooks"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "29",
    "Title": "The Last North Wind",
    "Isbn13": "9780000000293",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2024-01-03T09:14:00Z",
    "UpdatedAtUtc": "2026-02-04T09:42:00Z",
    "Authors": [
      "Ebba Norberg"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "30",
    "Title": "Echoes of Glass Planet",
    "Isbn13": "9780000000309",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2001,
    "CreatedAtUtc": "2023-02-24T06:20:00Z",
    "UpdatedAtUtc": "2026-01-09T22:11:00Z",
    "Authors": [
      "Frida Engström",
      "Alva Dahl"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "31",
    "Title": "Silent Thaw",
    "Isbn13": "9780000000316",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2003,
    "CreatedAtUtc": "2023-04-10T07:23:00Z",
    "UpdatedAtUtc": "2026-01-05T00:45:00Z",
    "Authors": [
      "Amelia Murray"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "32",
    "Title": "The Witness",
    "Isbn13": "9780000000323",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 1991,
    "CreatedAtUtc": "2023-10-10T15:30:00Z",
    "UpdatedAtUtc": "2026-02-06T05:03:00Z",
    "Authors": [
      "Elsa Norberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "33",
    "Title": "The Last Cartographer's Daughter",
    "Isbn13": "9780000000330",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2025-01-05T04:51:00Z",
    "UpdatedAtUtc": "2026-03-05T02:15:00Z",
    "Authors": [
      "Emily Wells"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "34",
    "Title": "The Last River School",
    "Isbn13": "9780000000347",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2024-08-15T09:55:00Z",
    "UpdatedAtUtc": "2026-03-07T09:36:00Z",
    "Authors": [
      "Elin Nyström"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "35",
    "Title": "Under Ashes",
    "Isbn13": "9780000000354",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2023-09-03T05:00:00Z",
    "UpdatedAtUtc": "2026-02-08T22:38:00Z",
    "Authors": [
      "Emily Turner"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "36",
    "Title": "The Last Skybound Sea",
    "Isbn13": "9780000000361",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 1999,
    "CreatedAtUtc": "2024-11-19T21:51:00Z",
    "UpdatedAtUtc": "2026-01-07T03:34:00Z",
    "Authors": [
      "Rachel Morgan"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "37",
    "Title": "Everyday Small Garden Year",
    "Isbn13": "9780000000378",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2025-10-10T14:07:00Z",
    "UpdatedAtUtc": "2026-02-05T22:25:00Z",
    "Authors": [
      "Daniel Hayes"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "38",
    "Title": "My River School",
    "Isbn13": "9780000000385",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2024-01-03T07:43:00Z",
    "UpdatedAtUtc": "2026-03-10T00:48:00Z",
    "Authors": [
      "Sara Sundin"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "39",
    "Title": "The Iron Moon",
    "Isbn13": "9780000000392",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 1996,
    "CreatedAtUtc": "2025-07-21T15:05:00Z",
    "UpdatedAtUtc": "2026-02-06T13:21:00Z",
    "Authors": [
      "Lovisa Lundqvist"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "40",
    "Title": "Practical Creative Habit Workbook",
    "Isbn13": "9780000000408",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2025-01-15T02:20:00Z",
    "UpdatedAtUtc": "2026-02-06T03:49:00Z",
    "Authors": [
      "Nora Dahl"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "41",
    "Title": "Adventures of Lantern Street",
    "Isbn13": "9780000000415",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2024-11-15T01:13:00Z",
    "UpdatedAtUtc": "2026-02-09T04:59:00Z",
    "Authors": [
      "Amelia Brooks"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "42",
    "Title": "The Northern Reform",
    "Isbn13": "9780000000422",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2025-07-03T07:53:00Z",
    "UpdatedAtUtc": "2026-01-08T03:41:00Z",
    "Authors": [
      "Thomas Turner",
      "Benjamin Lawson"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "43",
    "Title": "Witness to Engineer of Light",
    "Isbn13": "9780000000439",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2024-09-05T12:12:00Z",
    "UpdatedAtUtc": "2026-03-09T23:56:00Z",
    "Authors": [
      "Samuel Parker"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "44",
    "Title": "After Long Road",
    "Isbn13": "9780000000446",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2022,
    "CreatedAtUtc": "2025-08-28T04:28:00Z",
    "UpdatedAtUtc": "2026-03-08T11:21:00Z",
    "Authors": [
      "Astrid Dahl",
      "Linnea Molin"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "45",
    "Title": "Adventures of Mapmaker's Secret",
    "Isbn13": "9780000000453",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2025-08-23T12:24:00Z",
    "UpdatedAtUtc": "2026-03-03T15:02:00Z",
    "Authors": [
      "Thomas Wells",
      "Clara Foster",
      "Jonathan Carter"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "46",
    "Title": "Summer at Fox in the Library",
    "Isbn13": "9780000000460",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2011,
    "CreatedAtUtc": "2025-03-03T15:50:00Z",
    "UpdatedAtUtc": "2026-02-06T19:44:00Z",
    "Authors": [
      "Matilda Engström",
      "Astrid Bergqvist"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "47",
    "Title": "The Complete Mindful Walking Guide",
    "Isbn13": "9780000000477",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2023-04-21T21:58:00Z",
    "UpdatedAtUtc": "2026-02-04T23:05:00Z",
    "Authors": [
      "Isabel Parker",
      "George Carter"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "48",
    "Title": "The Quiet Map of Home",
    "Isbn13": "9780000000484",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 1988,
    "CreatedAtUtc": "2024-06-12T13:09:00Z",
    "UpdatedAtUtc": "2026-01-09T13:36:00Z",
    "Authors": [
      "Linnea Lindberg",
      "Karin Sundin"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "49",
    "Title": "Dark Harbor Case",
    "Isbn13": "9780000000491",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2024-10-05T07:29:00Z",
    "UpdatedAtUtc": "2026-03-05T14:16:00Z",
    "Authors": [
      "Clara Murray"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "50",
    "Title": "Before Glass House",
    "Isbn13": "9780000000507",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 2013,
    "CreatedAtUtc": "2024-10-10T20:27:00Z",
    "UpdatedAtUtc": "2026-03-05T14:54:00Z",
    "Authors": [
      "Ingrid Ekman"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "51",
    "Title": "The Seventh Harbor Case",
    "Isbn13": "9780000000514",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2024-12-10T00:53:00Z",
    "UpdatedAtUtc": "2026-03-07T08:00:00Z",
    "Authors": [
      "Saga Björk"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "52",
    "Title": "Working Calm Kitchen",
    "Isbn13": "9780000000521",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2001,
    "CreatedAtUtc": "2025-12-25T21:43:00Z",
    "UpdatedAtUtc": "2026-01-02T20:41:00Z",
    "Authors": [
      "David Foster",
      "Olivia Murray"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "53",
    "Title": "Echoes of Memory Colony",
    "Isbn13": "9780000000538",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2011,
    "CreatedAtUtc": "2023-04-05T17:56:00Z",
    "UpdatedAtUtc": "2026-02-09T16:58:00Z",
    "Authors": [
      "Linnea Sundin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "54",
    "Title": "The Seventh Snow Line",
    "Isbn13": "9780000000545",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 1999,
    "CreatedAtUtc": "2025-12-22T12:54:00Z",
    "UpdatedAtUtc": "2026-03-06T02:50:00Z",
    "Authors": [
      "Michael Bishop",
      "Emily Reed"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "55",
    "Title": "Songs of Harbor",
    "Isbn13": "9780000000552",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2009,
    "CreatedAtUtc": "2025-06-04T21:14:00Z",
    "UpdatedAtUtc": "2026-02-01T19:56:00Z",
    "Authors": [
      "Alva Molin"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "56",
    "Title": "The Last Frost Signal",
    "Isbn13": "9780000000569",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2011,
    "CreatedAtUtc": "2023-03-02T01:19:00Z",
    "UpdatedAtUtc": "2026-02-02T03:15:00Z",
    "Authors": [
      "Lovisa Dahl"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "57",
    "Title": "The Seventh Dead Letter",
    "Isbn13": "9780000000576",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2022,
    "CreatedAtUtc": "2025-12-05T13:41:00Z",
    "UpdatedAtUtc": "2026-01-08T19:26:00Z",
    "Authors": [
      "Tove Åkesson"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "58",
    "Title": "When River",
    "Isbn13": "9780000000583",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 1991,
    "CreatedAtUtc": "2025-06-18T20:22:00Z",
    "UpdatedAtUtc": "2026-01-07T08:12:00Z",
    "Authors": [
      "Ebba Björk"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "59",
    "Title": "Beyond Summer Republic",
    "Isbn13": "9780000000590",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1988,
    "CreatedAtUtc": "2024-04-05T18:13:00Z",
    "UpdatedAtUtc": "2026-01-09T06:37:00Z",
    "Authors": [
      "Nathan Bennett"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "60",
    "Title": "Dark Ash District",
    "Isbn13": "9780000000606",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2019,
    "CreatedAtUtc": "2024-03-04T21:55:00Z",
    "UpdatedAtUtc": "2026-01-03T00:22:00Z",
    "Authors": [
      "Sarah Reed"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "61",
    "Title": "The Sea Glass Summer",
    "Isbn13": "9780000000613",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2023-12-03T15:28:00Z",
    "UpdatedAtUtc": "2026-02-09T18:06:00Z",
    "Authors": [
      "Sarah Spencer"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "62",
    "Title": "The Snowy Hill",
    "Isbn13": "9780000000620",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 1988,
    "CreatedAtUtc": "2024-07-14T21:06:00Z",
    "UpdatedAtUtc": "2026-02-08T02:57:00Z",
    "Authors": [
      "Amelia Hayes",
      "Rachel Bennett"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "63",
    "Title": "Legends of Amber Forest",
    "Isbn13": "9780000000637",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2020,
    "CreatedAtUtc": "2025-06-13T19:33:00Z",
    "UpdatedAtUtc": "2026-02-08T16:38:00Z",
    "Authors": [
      "Elin Molin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "64",
    "Title": "The Quiet Ashes",
    "Isbn13": "9780000000644",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2011,
    "CreatedAtUtc": "2024-08-13T13:46:00Z",
    "UpdatedAtUtc": "2026-01-06T13:20:00Z",
    "Authors": [
      "Olivia Spencer",
      "Rachel Foster"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "65",
    "Title": "Legends of Frost Signal",
    "Isbn13": "9780000000651",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2008,
    "CreatedAtUtc": "2023-09-02T18:35:00Z",
    "UpdatedAtUtc": "2026-03-06T21:07:00Z",
    "Authors": [
      "Emily Morgan",
      "Jonathan Ellis"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "66",
    "Title": "The Skybound Sea",
    "Isbn13": "9780000000668",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2025-04-05T21:30:00Z",
    "UpdatedAtUtc": "2026-01-02T11:54:00Z",
    "Authors": [
      "David Ellis"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "67",
    "Title": "A Warden's Gate",
    "Isbn13": "9780000000675",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2025-11-21T17:01:00Z",
    "UpdatedAtUtc": "2026-03-05T00:11:00Z",
    "Authors": [
      "Jonathan Lawson"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "68",
    "Title": "Everyday Creative Habit Workbook",
    "Isbn13": "9780000000682",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2025-07-03T04:47:00Z",
    "UpdatedAtUtc": "2026-03-01T02:47:00Z",
    "Authors": [
      "Ingrid Bergqvist"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "69",
    "Title": "The Last Black Pier",
    "Isbn13": "9780000000699",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2025-10-03T01:09:00Z",
    "UpdatedAtUtc": "2026-01-10T01:43:00Z",
    "Authors": [
      "Frida Dahl"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "70",
    "Title": "Children of Hidden Orbit",
    "Isbn13": "9780000000705",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2025-02-12T13:07:00Z",
    "UpdatedAtUtc": "2026-02-10T15:33:00Z",
    "Authors": [
      "Jonathan Harper"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "71",
    "Title": "The Last Glass Planet",
    "Isbn13": "9780000000712",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2001,
    "CreatedAtUtc": "2024-06-04T00:31:00Z",
    "UpdatedAtUtc": "2026-03-07T05:08:00Z",
    "Authors": [
      "Sofia Dahl",
      "Sofia Bergqvist"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "72",
    "Title": "The Secret of Paper Dragon",
    "Isbn13": "9780000000729",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2025-01-14T00:29:00Z",
    "UpdatedAtUtc": "2026-01-06T18:27:00Z",
    "Authors": [
      "Frida Ekman"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "73",
    "Title": "Portrait of Cartographer's Daughter",
    "Isbn13": "9780000000736",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2024-12-12T02:27:00Z",
    "UpdatedAtUtc": "2026-01-04T13:37:00Z",
    "Authors": [
      "Isabel Turner"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "74",
    "Title": "The Last Blue Bicycle",
    "Isbn13": "9780000000743",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2025-04-25T11:22:00Z",
    "UpdatedAtUtc": "2026-03-03T07:06:00Z",
    "Authors": [
      "Daniel Morgan",
      "Amelia Ellis"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "75",
    "Title": "Legends of Warden's Gate",
    "Isbn13": "9780000000750",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2024-08-25T18:48:00Z",
    "UpdatedAtUtc": "2026-03-08T21:59:00Z",
    "Authors": [
      "Emily Turner"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "76",
    "Title": "The Complete Small Garden Year",
    "Isbn13": "9780000000767",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2024-05-19T01:22:00Z",
    "UpdatedAtUtc": "2026-03-02T09:29:00Z",
    "Authors": [
      "Nora Engström"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "77",
    "Title": "When Glass House",
    "Isbn13": "9780000000774",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2025-07-15T18:35:00Z",
    "UpdatedAtUtc": "2026-03-01T14:58:00Z",
    "Authors": [
      "Lena Viklund"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "78",
    "Title": "The Complete Simple Sewing Book",
    "Isbn13": "9780000000781",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2013,
    "CreatedAtUtc": "2023-06-23T02:32:00Z",
    "UpdatedAtUtc": "2026-03-03T01:15:00Z",
    "Authors": [
      "Elsa Sjöström"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "79",
    "Title": "Witness to Mayor's Notebook",
    "Isbn13": "9780000000798",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2003,
    "CreatedAtUtc": "2024-07-25T10:43:00Z",
    "UpdatedAtUtc": "2026-03-01T20:41:00Z",
    "Authors": [
      "David Brooks"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "80",
    "Title": "A City Shore",
    "Isbn13": "9780000000804",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2023-06-03T18:42:00Z",
    "UpdatedAtUtc": "2026-01-06T09:41:00Z",
    "Authors": [
      "Linnea Lundqvist"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "81",
    "Title": "Practical Simple Sewing Book",
    "Isbn13": "9780000000811",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2020,
    "CreatedAtUtc": "2024-11-26T10:52:00Z",
    "UpdatedAtUtc": "2026-01-09T02:41:00Z",
    "Authors": [
      "Emily Hayes"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "82",
    "Title": "Portrait of Northern Reform",
    "Isbn13": "9780000000828",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2006,
    "CreatedAtUtc": "2024-04-08T04:09:00Z",
    "UpdatedAtUtc": "2026-01-05T03:32:00Z",
    "Authors": [
      "Ingrid Sandberg"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "83",
    "Title": "The Mindful Walking Guide",
    "Isbn13": "9780000000835",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2024-03-06T05:53:00Z",
    "UpdatedAtUtc": "2026-03-10T05:46:00Z",
    "Authors": [
      "Elin Bergqvist"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "84",
    "Title": "When Ashes",
    "Isbn13": "9780000000842",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2003,
    "CreatedAtUtc": "2025-08-08T17:15:00Z",
    "UpdatedAtUtc": "2026-02-08T06:23:00Z",
    "Authors": [
      "Rachel Murray"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "85",
    "Title": "Adventures of Blue Bicycle",
    "Isbn13": "9780000000859",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 1993,
    "CreatedAtUtc": "2024-01-21T15:55:00Z",
    "UpdatedAtUtc": "2026-02-09T03:45:00Z",
    "Authors": [
      "Sara Nordin",
      "Sofia Viklund"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "86",
    "Title": "A Birches",
    "Isbn13": "9780000000866",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 2012,
    "CreatedAtUtc": "2023-04-27T14:56:00Z",
    "UpdatedAtUtc": "2026-02-01T13:03:00Z",
    "Authors": [
      "Amelia Reed"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "87",
    "Title": "The Secret of Lantern Street",
    "Isbn13": "9780000000873",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 1994,
    "CreatedAtUtc": "2023-01-10T15:44:00Z",
    "UpdatedAtUtc": "2026-01-08T14:39:00Z",
    "Authors": [
      "Astrid Sundin",
      "Maja Sundin"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "88",
    "Title": "Between River",
    "Isbn13": "9780000000880",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 2003,
    "CreatedAtUtc": "2023-05-04T01:15:00Z",
    "UpdatedAtUtc": "2026-02-10T14:04:00Z",
    "Authors": [
      "Nathan Griffin",
      "Jonathan Ellis"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "89",
    "Title": "Beyond Mayor's Notebook",
    "Isbn13": "9780000000897",
    "Language": "Danish",
    "Publisher": "Bloomsbury",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2025-06-08T18:26:00Z",
    "UpdatedAtUtc": "2026-01-02T16:23:00Z",
    "Authors": [
      "Linnea Åkesson"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "90",
    "Title": "The Lantern Street",
    "Isbn13": "9780000000903",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2007,
    "CreatedAtUtc": "2023-06-08T23:42:00Z",
    "UpdatedAtUtc": "2026-03-02T18:47:00Z",
    "Authors": [
      "Anna Björk",
      "Alva Lindberg"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "91",
    "Title": "Hidden Final Alibi",
    "Isbn13": "9780000000910",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2015,
    "CreatedAtUtc": "2025-03-26T04:04:00Z",
    "UpdatedAtUtc": "2026-03-08T01:18:00Z",
    "Authors": [
      "Ingrid Engström"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "92",
    "Title": "The Long Road",
    "Isbn13": "9780000000927",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 1997,
    "CreatedAtUtc": "2024-06-28T01:55:00Z",
    "UpdatedAtUtc": "2026-03-06T08:07:00Z",
    "Authors": [
      "George Parker",
      "Hannah Carter"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "93",
    "Title": "Beyond Century of Bread",
    "Isbn13": "9780000000934",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2024-02-24T13:05:00Z",
    "UpdatedAtUtc": "2026-02-10T05:34:00Z",
    "Authors": [
      "Daniel Parker",
      "Samuel Brooks"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "94",
    "Title": "Chronicles of Ash Kingdom",
    "Isbn13": "9780000000941",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2025-07-06T22:28:00Z",
    "UpdatedAtUtc": "2026-02-08T01:46:00Z",
    "Authors": [
      "Linnea Engström"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "95",
    "Title": "The Curious Cinnamon Trail",
    "Isbn13": "9780000000958",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 1994,
    "CreatedAtUtc": "2025-11-26T14:02:00Z",
    "UpdatedAtUtc": "2026-01-02T07:49:00Z",
    "Authors": [
      "Lena Wallin",
      "Frida Hedlund",
      "Ingrid Lindberg"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "96",
    "Title": "Children of Warden's Gate",
    "Isbn13": "9780000000965",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2008,
    "CreatedAtUtc": "2024-08-25T02:36:00Z",
    "UpdatedAtUtc": "2026-01-09T11:25:00Z",
    "Authors": [
      "Elsa Engström"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "97",
    "Title": "The Modern Home Budget Handbook",
    "Isbn13": "9780000000972",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2024-09-04T08:49:00Z",
    "UpdatedAtUtc": "2026-02-08T06:39:00Z",
    "Authors": [
      "Ingrid Norberg"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "98",
    "Title": "Practical Home Budget Handbook",
    "Isbn13": "9780000000989",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2025-06-23T02:35:00Z",
    "UpdatedAtUtc": "2026-03-05T09:54:00Z",
    "Authors": [
      "Rachel Morgan"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "99",
    "Title": "The Modern Small Garden Year",
    "Isbn13": "9780000000996",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2008,
    "CreatedAtUtc": "2025-10-12T14:51:00Z",
    "UpdatedAtUtc": "2026-03-03T19:56:00Z",
    "Authors": [
      "Nora Lindberg"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "100",
    "Title": "After Map of Home",
    "Isbn13": "9780000001009",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2011,
    "CreatedAtUtc": "2025-02-05T10:41:00Z",
    "UpdatedAtUtc": "2026-01-08T14:43:00Z",
    "Authors": [
      "Matilda Åkesson"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "101",
    "Title": "Legends of Iron Moon",
    "Isbn13": "9780000001016",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2023-05-06T05:20:00Z",
    "UpdatedAtUtc": "2026-03-04T11:33:00Z",
    "Authors": [
      "Karin Holm"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "102",
    "Title": "Songs of Glass House",
    "Isbn13": "9780000001023",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2019,
    "CreatedAtUtc": "2023-09-21T05:37:00Z",
    "UpdatedAtUtc": "2026-03-03T05:42:00Z",
    "Authors": [
      "Benjamin Murray"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "103",
    "Title": "The Guide to Sourdough",
    "Isbn13": "9780000001030",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2025-07-20T20:01:00Z",
    "UpdatedAtUtc": "2026-02-09T09:41:00Z",
    "Authors": [
      "Eleanor Harper"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "104",
    "Title": "The Last River Campaign",
    "Isbn13": "9780000001047",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 1995,
    "CreatedAtUtc": "2024-07-16T14:13:00Z",
    "UpdatedAtUtc": "2026-02-10T04:20:00Z",
    "Authors": [
      "Emily Carter"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "105",
    "Title": "Legends of Ash Kingdom",
    "Isbn13": "9780000001054",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2024-02-09T14:15:00Z",
    "UpdatedAtUtc": "2026-01-02T01:18:00Z",
    "Authors": [
      "Maja Sundin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "106",
    "Title": "The Secret of Sea Glass Summer",
    "Isbn13": "9780000001061",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2024-08-01T02:25:00Z",
    "UpdatedAtUtc": "2026-03-08T07:13:00Z",
    "Authors": [
      "Emilia Sandberg",
      "Ingrid Norberg",
      "Matilda Engström"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "107",
    "Title": "The Warden's Gate",
    "Isbn13": "9780000001078",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 1991,
    "CreatedAtUtc": "2024-03-09T23:23:00Z",
    "UpdatedAtUtc": "2026-02-06T01:25:00Z",
    "Authors": [
      "Nora Dahl",
      "Tove Lindberg"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "108",
    "Title": "The Secret of Mapmaker's Secret",
    "Isbn13": "9780000001085",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2017,
    "CreatedAtUtc": "2024-09-09T19:43:00Z",
    "UpdatedAtUtc": "2026-03-02T04:06:00Z",
    "Authors": [
      "Lena Wallin"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "109",
    "Title": "Echoes of Night Engine",
    "Isbn13": "9780000001092",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2017,
    "CreatedAtUtc": "2023-01-02T04:45:00Z",
    "UpdatedAtUtc": "2026-02-08T16:29:00Z",
    "Authors": [
      "Sofia Viklund",
      "Matilda Wallin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "110",
    "Title": "A Mapmaker's Secret",
    "Isbn13": "9780000001108",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2006,
    "CreatedAtUtc": "2025-09-18T15:45:00Z",
    "UpdatedAtUtc": "2026-03-05T15:52:00Z",
    "Authors": [
      "Anna Viklund",
      "Linnea Molin"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "111",
    "Title": "Chronicles of Glass Planet",
    "Isbn13": "9780000001115",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2015,
    "CreatedAtUtc": "2024-11-26T18:36:00Z",
    "UpdatedAtUtc": "2026-01-01T18:30:00Z",
    "Authors": [
      "Astrid Viklund"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "112",
    "Title": "The Last Sea Glass Summer",
    "Isbn13": "9780000001122",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2013,
    "CreatedAtUtc": "2024-07-05T13:44:00Z",
    "UpdatedAtUtc": "2026-01-07T16:49:00Z",
    "Authors": [
      "Saga Holm",
      "Sofia Lindberg"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "113",
    "Title": "Beyond Years of Smoke",
    "Isbn13": "9780000001139",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2015,
    "CreatedAtUtc": "2025-07-11T05:29:00Z",
    "UpdatedAtUtc": "2026-03-06T17:22:00Z",
    "Authors": [
      "George Coleman"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "114",
    "Title": "Working Digital Declutter Plan",
    "Isbn13": "9780000001146",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2025-12-16T10:30:00Z",
    "UpdatedAtUtc": "2026-02-09T23:17:00Z",
    "Authors": [
      "Benjamin Foster",
      "Benjamin Hayes"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "115",
    "Title": "The Quiet City Shore",
    "Isbn13": "9780000001153",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 1994,
    "CreatedAtUtc": "2024-01-10T22:05:00Z",
    "UpdatedAtUtc": "2026-02-08T20:16:00Z",
    "Authors": [
      "Anna Björk"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "116",
    "Title": "A Mayor's Notebook",
    "Isbn13": "9780000001160",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 1993,
    "CreatedAtUtc": "2023-10-24T18:15:00Z",
    "UpdatedAtUtc": "2026-01-01T21:57:00Z",
    "Authors": [
      "George Harper"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "117",
    "Title": "The Harbor Case",
    "Isbn13": "9780000001177",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 1993,
    "CreatedAtUtc": "2023-09-06T13:41:00Z",
    "UpdatedAtUtc": "2026-02-08T20:12:00Z",
    "Authors": [
      "Samuel Ellis"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "118",
    "Title": "The Amber Forest",
    "Isbn13": "9780000001184",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2023-07-27T05:59:00Z",
    "UpdatedAtUtc": "2026-01-07T15:11:00Z",
    "Authors": [
      "Ebba Öhman"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "119",
    "Title": "A Hidden Orbit",
    "Isbn13": "9780000001191",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2023-02-11T05:46:00Z",
    "UpdatedAtUtc": "2026-02-05T22:11:00Z",
    "Authors": [
      "Samuel Reed",
      "Amelia Bishop"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "120",
    "Title": "Everyday Simple Sewing Book",
    "Isbn13": "9780000001207",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2025-07-27T13:32:00Z",
    "UpdatedAtUtc": "2026-02-02T12:42:00Z",
    "Authors": [
      "Olivia Turner"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "121",
    "Title": "Cold Witness",
    "Isbn13": "9780000001214",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2024-05-19T23:36:00Z",
    "UpdatedAtUtc": "2026-01-05T20:23:00Z",
    "Authors": [
      "Ebba Engström"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "122",
    "Title": "A Black Pier",
    "Isbn13": "9780000001221",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2004,
    "CreatedAtUtc": "2025-02-21T09:23:00Z",
    "UpdatedAtUtc": "2026-03-04T07:08:00Z",
    "Authors": [
      "Anna Hedlund"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "123",
    "Title": "The Burning Dead Letter",
    "Isbn13": "9780000001238",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2019,
    "CreatedAtUtc": "2025-04-25T07:43:00Z",
    "UpdatedAtUtc": "2026-03-02T16:28:00Z",
    "Authors": [
      "Tove Norberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "124",
    "Title": "A Simple Sewing Book",
    "Isbn13": "9780000001245",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 1997,
    "CreatedAtUtc": "2025-09-05T05:20:00Z",
    "UpdatedAtUtc": "2026-03-08T03:43:00Z",
    "Authors": [
      "Tove Hedlund"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "125",
    "Title": "Working Guide to Sourdough",
    "Isbn13": "9780000001252",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2014,
    "CreatedAtUtc": "2025-01-18T14:43:00Z",
    "UpdatedAtUtc": "2026-02-01T12:16:00Z",
    "Authors": [
      "Amelia Spencer"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "126",
    "Title": "A Guide to Sourdough",
    "Isbn13": "9780000001269",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 1988,
    "CreatedAtUtc": "2023-08-02T09:26:00Z",
    "UpdatedAtUtc": "2026-01-03T20:46:00Z",
    "Authors": [
      "Lena Öhman"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "127",
    "Title": "Lives of Century of Bread",
    "Isbn13": "9780000001276",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 1996,
    "CreatedAtUtc": "2025-07-17T04:46:00Z",
    "UpdatedAtUtc": "2026-01-01T00:19:00Z",
    "Authors": [
      "Anna Ekman",
      "Tove Bergqvist",
      "Frida Holm"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "128",
    "Title": "Smart Better Sleep Manual",
    "Isbn13": "9780000001283",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2014,
    "CreatedAtUtc": "2024-03-09T06:59:00Z",
    "UpdatedAtUtc": "2026-03-02T01:51:00Z",
    "Authors": [
      "Ebba Nyström"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "129",
    "Title": "The Last Northern Reform",
    "Isbn13": "9780000001290",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2023-08-20T21:45:00Z",
    "UpdatedAtUtc": "2026-01-04T23:02:00Z",
    "Authors": [
      "Lena Holm"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "130",
    "Title": "Portrait of Summer Republic",
    "Isbn13": "9780000001306",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2024-03-22T16:14:00Z",
    "UpdatedAtUtc": "2026-02-05T08:03:00Z",
    "Authors": [
      "Emilia Molin",
      "Elin Sundin"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "131",
    "Title": "The Last Paper Dragon",
    "Isbn13": "9780000001313",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2024-12-14T13:09:00Z",
    "UpdatedAtUtc": "2026-02-07T05:48:00Z",
    "Authors": [
      "David Ward"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "132",
    "Title": "The Last Summer Republic",
    "Isbn13": "9780000001320",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1988,
    "CreatedAtUtc": "2025-07-14T17:33:00Z",
    "UpdatedAtUtc": "2026-01-07T07:16:00Z",
    "Authors": [
      "Elsa Engström"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "133",
    "Title": "Chronicles of Hidden Orbit",
    "Isbn13": "9780000001337",
    "Language": "Danish",
    "Publisher": "Vintage",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2023-04-26T18:46:00Z",
    "UpdatedAtUtc": "2026-03-09T06:30:00Z",
    "Authors": [
      "Karin Lundqvist",
      "Nathan Murray"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "134",
    "Title": "The Fifth Archive",
    "Isbn13": "9780000001344",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2025-06-25T09:24:00Z",
    "UpdatedAtUtc": "2026-02-09T20:22:00Z",
    "Authors": [
      "Maja Norberg",
      "Ebba Viklund",
      "Sofia Wallin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "135",
    "Title": "The Hidden Orbit",
    "Isbn13": "9780000001351",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2024-06-14T01:36:00Z",
    "UpdatedAtUtc": "2026-01-03T00:16:00Z",
    "Authors": [
      "Samuel Coleman"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "136",
    "Title": "The Curious Sea Glass Summer",
    "Isbn13": "9780000001368",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2023-08-18T20:43:00Z",
    "UpdatedAtUtc": "2026-02-05T15:46:00Z",
    "Authors": [
      "Thomas Ward"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "137",
    "Title": "The Better Sleep Manual",
    "Isbn13": "9780000001375",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2024-03-27T20:14:00Z",
    "UpdatedAtUtc": "2026-01-03T00:13:00Z",
    "Authors": [
      "Emily Carter"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "138",
    "Title": "The Quiet Resistance",
    "Isbn13": "9780000001382",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2023-01-18T17:26:00Z",
    "UpdatedAtUtc": "2026-01-01T16:46:00Z",
    "Authors": [
      "Elin Norberg"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "139",
    "Title": "My Fox in the Library",
    "Isbn13": "9780000001399",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2025-05-27T11:17:00Z",
    "UpdatedAtUtc": "2026-02-02T11:25:00Z",
    "Authors": [
      "Ebba Sandberg"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "140",
    "Title": "The Secret of Blue Bicycle",
    "Isbn13": "9780000001405",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 1990,
    "CreatedAtUtc": "2024-07-13T17:30:00Z",
    "UpdatedAtUtc": "2026-01-01T22:10:00Z",
    "Authors": [
      "Lena Sjöström"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "141",
    "Title": "Beyond Hidden Court",
    "Isbn13": "9780000001412",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2023-04-28T22:56:00Z",
    "UpdatedAtUtc": "2026-03-08T08:02:00Z",
    "Authors": [
      "Michael Griffin"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "142",
    "Title": "The Small Garden Year",
    "Isbn13": "9780000001429",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 1989,
    "CreatedAtUtc": "2024-03-19T07:36:00Z",
    "UpdatedAtUtc": "2026-02-09T10:24:00Z",
    "Authors": [
      "Sofia Molin",
      "Elsa Wallin"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "143",
    "Title": "A Final Alibi",
    "Isbn13": "9780000001436",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2006,
    "CreatedAtUtc": "2025-10-20T12:49:00Z",
    "UpdatedAtUtc": "2026-02-01T20:17:00Z",
    "Authors": [
      "Maja Åkesson",
      "Ebba Ekman"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "144",
    "Title": "Portrait of Mayor's Notebook",
    "Isbn13": "9780000001443",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2003,
    "CreatedAtUtc": "2023-12-03T02:17:00Z",
    "UpdatedAtUtc": "2026-01-07T22:50:00Z",
    "Authors": [
      "Saga Wallin",
      "Lena Viklund"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "145",
    "Title": "The Last Dead Letter",
    "Isbn13": "9780000001450",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2004,
    "CreatedAtUtc": "2024-06-25T08:06:00Z",
    "UpdatedAtUtc": "2026-01-02T05:27:00Z",
    "Authors": [
      "Lena Lindberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "146",
    "Title": "The Last Fox in the Library",
    "Isbn13": "9780000001467",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 1990,
    "CreatedAtUtc": "2025-10-26T10:55:00Z",
    "UpdatedAtUtc": "2026-02-01T09:26:00Z",
    "Authors": [
      "Frida Öhman"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "147",
    "Title": "Letters from City Shore",
    "Isbn13": "9780000001474",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 1993,
    "CreatedAtUtc": "2024-05-09T15:09:00Z",
    "UpdatedAtUtc": "2026-01-03T13:17:00Z",
    "Authors": [
      "Clara Turner"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "148",
    "Title": "A Fifth Archive",
    "Isbn13": "9780000001481",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 1993,
    "CreatedAtUtc": "2024-01-13T10:53:00Z",
    "UpdatedAtUtc": "2026-03-07T10:28:00Z",
    "Authors": [
      "Elin Sandberg",
      "Lovisa Holm"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "149",
    "Title": "Portrait of Hidden Court",
    "Isbn13": "9780000001498",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2023-07-11T09:47:00Z",
    "UpdatedAtUtc": "2026-03-08T18:50:00Z",
    "Authors": [
      "Olivia Parker"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "150",
    "Title": "A Glass Planet",
    "Isbn13": "9780000001504",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2022,
    "CreatedAtUtc": "2025-03-22T17:01:00Z",
    "UpdatedAtUtc": "2026-03-08T22:13:00Z",
    "Authors": [
      "Saga Sandberg"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "151",
    "Title": "Children of Frost Signal",
    "Isbn13": "9780000001511",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2001,
    "CreatedAtUtc": "2025-03-23T13:50:00Z",
    "UpdatedAtUtc": "2026-02-02T14:38:00Z",
    "Authors": [
      "Benjamin Foster"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "152",
    "Title": "The Mapmaker's Secret",
    "Isbn13": "9780000001528",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2023-02-25T07:42:00Z",
    "UpdatedAtUtc": "2026-03-06T05:41:00Z",
    "Authors": [
      "Tove Viklund"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "153",
    "Title": "When North Wind",
    "Isbn13": "9780000001535",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 1999,
    "CreatedAtUtc": "2025-12-17T17:37:00Z",
    "UpdatedAtUtc": "2026-03-10T07:28:00Z",
    "Authors": [
      "Michael Harper"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "154",
    "Title": "The Last Quiet Resistance",
    "Isbn13": "9780000001542",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 1986,
    "CreatedAtUtc": "2024-02-10T13:58:00Z",
    "UpdatedAtUtc": "2026-01-02T23:09:00Z",
    "Authors": [
      "Elsa Björk"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "155",
    "Title": "The Memory Colony",
    "Isbn13": "9780000001559",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2004,
    "CreatedAtUtc": "2023-12-04T00:54:00Z",
    "UpdatedAtUtc": "2026-02-02T21:51:00Z",
    "Authors": [
      "Isabel Morgan"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "156",
    "Title": "Practical Better Sleep Manual",
    "Isbn13": "9780000001566",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2014,
    "CreatedAtUtc": "2023-07-21T05:11:00Z",
    "UpdatedAtUtc": "2026-03-07T12:01:00Z",
    "Authors": [
      "George Spencer"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "157",
    "Title": "Adventures of River School",
    "Isbn13": "9780000001573",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2024-12-26T02:36:00Z",
    "UpdatedAtUtc": "2026-01-09T05:23:00Z",
    "Authors": [
      "Astrid Sandberg"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "158",
    "Title": "A Snow Line",
    "Isbn13": "9780000001580",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2024-10-13T19:25:00Z",
    "UpdatedAtUtc": "2026-03-02T11:22:00Z",
    "Authors": [
      "Nora Hedlund"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "159",
    "Title": "The Curious River School",
    "Isbn13": "9780000001597",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 1992,
    "CreatedAtUtc": "2023-05-04T05:23:00Z",
    "UpdatedAtUtc": "2026-03-03T16:24:00Z",
    "Authors": [
      "Elsa Sundin"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "160",
    "Title": "The Last Lantern Street",
    "Isbn13": "9780000001603",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2020,
    "CreatedAtUtc": "2023-08-10T04:11:00Z",
    "UpdatedAtUtc": "2026-02-08T19:03:00Z",
    "Authors": [
      "Tove Nordin"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "161",
    "Title": "Beyond River",
    "Isbn13": "9780000001610",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2023-02-19T00:50:00Z",
    "UpdatedAtUtc": "2026-01-05T01:17:00Z",
    "Authors": [
      "Amelia Parker",
      "Jonathan Parker",
      "Jonathan Bishop"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "162",
    "Title": "A Moonlight Camp",
    "Isbn13": "9780000001627",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2017,
    "CreatedAtUtc": "2023-10-04T08:49:00Z",
    "UpdatedAtUtc": "2026-03-06T17:52:00Z",
    "Authors": [
      "Samuel Lawson"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "163",
    "Title": "The Modern Nordic Woodshop",
    "Isbn13": "9780000001634",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2024-05-12T02:28:00Z",
    "UpdatedAtUtc": "2026-01-04T06:51:00Z",
    "Authors": [
      "Ebba Dahl"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "164",
    "Title": "A Midnight Notebook",
    "Isbn13": "9780000001641",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2025-03-11T22:18:00Z",
    "UpdatedAtUtc": "2026-02-10T08:54:00Z",
    "Authors": [
      "Eleanor Brooks",
      "George Harper"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "165",
    "Title": "A Small Garden Year",
    "Isbn13": "9780000001658",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2023-12-11T07:48:00Z",
    "UpdatedAtUtc": "2026-03-07T15:09:00Z",
    "Authors": [
      "Karin Lundqvist",
      "Elsa Bergqvist"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "166",
    "Title": "Smart Nordic Woodshop",
    "Isbn13": "9780000001665",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2017,
    "CreatedAtUtc": "2025-05-23T11:29:00Z",
    "UpdatedAtUtc": "2026-02-06T18:00:00Z",
    "Authors": [
      "Emily Ward"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "167",
    "Title": "Before Ashes",
    "Isbn13": "9780000001672",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2019,
    "CreatedAtUtc": "2024-04-14T06:31:00Z",
    "UpdatedAtUtc": "2026-02-06T09:21:00Z",
    "Authors": [
      "Frida Ekman"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "168",
    "Title": "The Clocktower Club",
    "Isbn13": "9780000001689",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 1992,
    "CreatedAtUtc": "2023-08-15T00:27:00Z",
    "UpdatedAtUtc": "2026-01-03T20:19:00Z",
    "Authors": [
      "Lovisa Lindberg"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "169",
    "Title": "Echoes of Skybound Sea",
    "Isbn13": "9780000001696",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2025-05-24T22:48:00Z",
    "UpdatedAtUtc": "2026-01-07T20:05:00Z",
    "Authors": [
      "Daniel Carter"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "170",
    "Title": "Under Midsummer Light",
    "Isbn13": "9780000001702",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2025-08-01T00:44:00Z",
    "UpdatedAtUtc": "2026-02-02T13:44:00Z",
    "Authors": [
      "Saga Nyström"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "171",
    "Title": "A River Campaign",
    "Isbn13": "9780000001719",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2024-05-05T12:49:00Z",
    "UpdatedAtUtc": "2026-01-03T02:33:00Z",
    "Authors": [
      "Maja Holm"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "172",
    "Title": "The Quiet Birches",
    "Isbn13": "9780000001726",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2011,
    "CreatedAtUtc": "2025-11-15T06:05:00Z",
    "UpdatedAtUtc": "2026-01-03T03:37:00Z",
    "Authors": [
      "Sofia Bergqvist"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "173",
    "Title": "Lives of Hidden Court",
    "Isbn13": "9780000001733",
    "Language": "Danish",
    "Publisher": "Ordfront",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2025-10-17T06:47:00Z",
    "UpdatedAtUtc": "2026-02-05T20:03:00Z",
    "Authors": [
      "Ebba Öhman",
      "Anna Hayes"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "174",
    "Title": "The Summer Republic",
    "Isbn13": "9780000001740",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2023-06-11T04:24:00Z",
    "UpdatedAtUtc": "2026-03-07T11:34:00Z",
    "Authors": [
      "Elin Ekman"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "175",
    "Title": "The Ash District",
    "Isbn13": "9780000001757",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2023-07-17T09:40:00Z",
    "UpdatedAtUtc": "2026-03-09T13:27:00Z",
    "Authors": [
      "Alva Sandberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "176",
    "Title": "Witness to Quiet Resistance",
    "Isbn13": "9780000001764",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 1990,
    "CreatedAtUtc": "2025-04-26T04:34:00Z",
    "UpdatedAtUtc": "2026-02-07T18:40:00Z",
    "Authors": [
      "Lena Dahl"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "177",
    "Title": "Everyday Tiny Balcony Garden",
    "Isbn13": "9780000001771",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2023-07-04T14:39:00Z",
    "UpdatedAtUtc": "2026-03-01T09:42:00Z",
    "Authors": [
      "Thomas Carter"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "178",
    "Title": "The Complete Nordic Woodshop",
    "Isbn13": "9780000001788",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2025-02-16T14:32:00Z",
    "UpdatedAtUtc": "2026-02-01T15:54:00Z",
    "Authors": [
      "Emily Harper"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "179",
    "Title": "Dark Snow Line",
    "Isbn13": "9780000001795",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2023-04-02T15:23:00Z",
    "UpdatedAtUtc": "2026-02-03T05:59:00Z",
    "Authors": [
      "Maja Nyström",
      "Matilda Lindberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "180",
    "Title": "Summer at Blue Bicycle",
    "Isbn13": "9780000001801",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 1999,
    "CreatedAtUtc": "2025-01-13T02:56:00Z",
    "UpdatedAtUtc": "2026-02-05T09:20:00Z",
    "Authors": [
      "Michael Griffin",
      "James Turner"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "181",
    "Title": "The Curious Snowy Hill",
    "Isbn13": "9780000001818",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2022,
    "CreatedAtUtc": "2024-04-24T17:48:00Z",
    "UpdatedAtUtc": "2026-01-03T00:59:00Z",
    "Authors": [
      "Rachel Brooks"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "182",
    "Title": "Songs of Long Road",
    "Isbn13": "9780000001825",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2025-07-28T23:46:00Z",
    "UpdatedAtUtc": "2026-02-02T06:33:00Z",
    "Authors": [
      "Astrid Sundin"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "183",
    "Title": "The Years of Smoke",
    "Isbn13": "9780000001832",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 1999,
    "CreatedAtUtc": "2025-07-23T11:29:00Z",
    "UpdatedAtUtc": "2026-03-02T18:06:00Z",
    "Authors": [
      "Lena Sjöström"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "184",
    "Title": "Ash District",
    "Isbn13": "9780000001849",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2000,
    "CreatedAtUtc": "2024-05-28T10:59:00Z",
    "UpdatedAtUtc": "2026-02-06T10:28:00Z",
    "Authors": [
      "Jonathan Reed"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "185",
    "Title": "Cold Winter File",
    "Isbn13": "9780000001856",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2014,
    "CreatedAtUtc": "2024-07-04T17:22:00Z",
    "UpdatedAtUtc": "2026-01-08T09:29:00Z",
    "Authors": [
      "Elsa Holm",
      "Ingrid Engström"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "186",
    "Title": "Beyond Ashes",
    "Isbn13": "9780000001863",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 1998,
    "CreatedAtUtc": "2024-03-03T22:15:00Z",
    "UpdatedAtUtc": "2026-02-07T16:03:00Z",
    "Authors": [
      "Elin Sjöström"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "187",
    "Title": "Legends of Starforge",
    "Isbn13": "9780000001870",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2014,
    "CreatedAtUtc": "2023-03-04T00:03:00Z",
    "UpdatedAtUtc": "2026-01-03T06:52:00Z",
    "Authors": [
      "George Lawson",
      "Thomas Ellis"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "188",
    "Title": "Chronicles of Warden's Gate",
    "Isbn13": "9780000001887",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 1989,
    "CreatedAtUtc": "2023-11-15T04:05:00Z",
    "UpdatedAtUtc": "2026-02-02T01:29:00Z",
    "Authors": [
      "Lena Lindberg"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "189",
    "Title": "The Last Thaw",
    "Isbn13": "9780000001894",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2012,
    "CreatedAtUtc": "2023-06-07T05:17:00Z",
    "UpdatedAtUtc": "2026-02-08T04:02:00Z",
    "Authors": [
      "Astrid Wallin"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "190",
    "Title": "The Secret of Cinnamon Trail",
    "Isbn13": "9780000001900",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2015,
    "CreatedAtUtc": "2023-02-09T12:08:00Z",
    "UpdatedAtUtc": "2026-02-04T20:56:00Z",
    "Authors": [
      "Sara Öhman"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "191",
    "Title": "The Missing Witness",
    "Isbn13": "9780000001917",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2007,
    "CreatedAtUtc": "2025-09-11T12:17:00Z",
    "UpdatedAtUtc": "2026-01-01T10:38:00Z",
    "Authors": [
      "Frida Norberg",
      "Tove Norberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "192",
    "Title": "Between Winter",
    "Isbn13": "9780000001924",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 1991,
    "CreatedAtUtc": "2023-11-08T08:34:00Z",
    "UpdatedAtUtc": "2026-03-01T07:36:00Z",
    "Authors": [
      "Frida Molin",
      "Ebba Nordin"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "193",
    "Title": "Legends of Fifth Archive",
    "Isbn13": "9780000001931",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 1986,
    "CreatedAtUtc": "2025-12-12T18:36:00Z",
    "UpdatedAtUtc": "2026-01-10T06:53:00Z",
    "Authors": [
      "David Wells"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "194",
    "Title": "The Fox in the Library",
    "Isbn13": "9780000001948",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 1986,
    "CreatedAtUtc": "2025-08-01T10:58:00Z",
    "UpdatedAtUtc": "2026-03-04T04:21:00Z",
    "Authors": [
      "Elin Nyström"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "195",
    "Title": "Hidden Witness",
    "Isbn13": "9780000001955",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 1992,
    "CreatedAtUtc": "2025-06-03T11:45:00Z",
    "UpdatedAtUtc": "2026-03-07T18:06:00Z",
    "Authors": [
      "Saga Bergqvist"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "196",
    "Title": "The Frost Signal",
    "Isbn13": "9780000001962",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2007,
    "CreatedAtUtc": "2023-11-19T05:24:00Z",
    "UpdatedAtUtc": "2026-02-05T04:11:00Z",
    "Authors": [
      "George Murray"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "197",
    "Title": "Letters from Winter",
    "Isbn13": "9780000001979",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 1999,
    "CreatedAtUtc": "2025-10-04T15:09:00Z",
    "UpdatedAtUtc": "2026-02-02T07:22:00Z",
    "Authors": [
      "Isabel Murray"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "198",
    "Title": "Hidden Black Pier",
    "Isbn13": "9780000001986",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 1989,
    "CreatedAtUtc": "2024-05-26T03:46:00Z",
    "UpdatedAtUtc": "2026-01-01T12:28:00Z",
    "Authors": [
      "Olivia Foster"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "199",
    "Title": "Witness to River Campaign",
    "Isbn13": "9780000001993",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1999,
    "CreatedAtUtc": "2023-08-03T01:18:00Z",
    "UpdatedAtUtc": "2026-01-06T08:06:00Z",
    "Authors": [
      "Nora Molin"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "200",
    "Title": "Children of Iron Moon",
    "Isbn13": "9780000002006",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2015,
    "CreatedAtUtc": "2025-01-14T20:10:00Z",
    "UpdatedAtUtc": "2026-03-07T05:03:00Z",
    "Authors": [
      "Lena Sundin",
      "Elin Viklund"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "201",
    "Title": "A Frost Signal",
    "Isbn13": "9780000002013",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 1988,
    "CreatedAtUtc": "2024-10-18T15:09:00Z",
    "UpdatedAtUtc": "2026-01-06T00:27:00Z",
    "Authors": [
      "Thomas Ellis"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "202",
    "Title": "The Last Amber Forest",
    "Isbn13": "9780000002020",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 1990,
    "CreatedAtUtc": "2024-02-10T12:30:00Z",
    "UpdatedAtUtc": "2026-02-05T02:41:00Z",
    "Authors": [
      "Daniel Hayes"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "203",
    "Title": "Portrait of River Campaign",
    "Isbn13": "9780000002037",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2001,
    "CreatedAtUtc": "2024-08-09T07:53:00Z",
    "UpdatedAtUtc": "2026-02-10T01:09:00Z",
    "Authors": [
      "Lovisa Sjöström"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "204",
    "Title": "A Mindful Walking Guide",
    "Isbn13": "9780000002044",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2022,
    "CreatedAtUtc": "2023-09-02T12:52:00Z",
    "UpdatedAtUtc": "2026-03-07T17:43:00Z",
    "Authors": [
      "Tove Åkesson"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "205",
    "Title": "My Lantern Street",
    "Isbn13": "9780000002051",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 1992,
    "CreatedAtUtc": "2024-03-06T06:51:00Z",
    "UpdatedAtUtc": "2026-01-03T01:27:00Z",
    "Authors": [
      "Matilda Molin",
      "Elin Bergqvist"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "206",
    "Title": "The Modern Mindful Walking Guide",
    "Isbn13": "9780000002068",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2025-02-03T12:35:00Z",
    "UpdatedAtUtc": "2026-02-09T10:54:00Z",
    "Authors": [
      "Nathan Harper"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "207",
    "Title": "A River School",
    "Isbn13": "9780000002075",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2023-05-28T17:56:00Z",
    "UpdatedAtUtc": "2026-02-07T08:24:00Z",
    "Authors": [
      "Maja Holm"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "208",
    "Title": "A Quiet Resistance",
    "Isbn13": "9780000002082",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2024-12-08T01:47:00Z",
    "UpdatedAtUtc": "2026-02-05T11:01:00Z",
    "Authors": [
      "Clara Carter"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "209",
    "Title": "Chronicles of Fifth Archive",
    "Isbn13": "9780000002099",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2025-07-20T20:05:00Z",
    "UpdatedAtUtc": "2026-01-08T06:58:00Z",
    "Authors": [
      "Rachel Coleman"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "210",
    "Title": "The Complete Guide to Sourdough",
    "Isbn13": "9780000002105",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2004,
    "CreatedAtUtc": "2025-09-13T04:49:00Z",
    "UpdatedAtUtc": "2026-03-10T00:11:00Z",
    "Authors": [
      "Ingrid Ekman"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "211",
    "Title": "The Black Pier",
    "Isbn13": "9780000002112",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2024-08-17T12:40:00Z",
    "UpdatedAtUtc": "2026-01-01T07:23:00Z",
    "Authors": [
      "Frida Sandberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "212",
    "Title": "Summer at Mapmaker's Secret",
    "Isbn13": "9780000002129",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2003,
    "CreatedAtUtc": "2023-07-02T05:54:00Z",
    "UpdatedAtUtc": "2026-03-04T07:41:00Z",
    "Authors": [
      "Alva Ekman"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "213",
    "Title": "Everyday Mindful Walking Guide",
    "Isbn13": "9780000002136",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2015,
    "CreatedAtUtc": "2023-11-06T19:51:00Z",
    "UpdatedAtUtc": "2026-01-04T08:38:00Z",
    "Authors": [
      "Anna Bennett"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "214",
    "Title": "The Complete Family Meal Planner",
    "Isbn13": "9780000002143",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2025-07-19T03:00:00Z",
    "UpdatedAtUtc": "2026-03-06T17:41:00Z",
    "Authors": [
      "Rachel Hayes"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "215",
    "Title": "My Moonlight Camp",
    "Isbn13": "9780000002150",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2024-11-23T20:19:00Z",
    "UpdatedAtUtc": "2026-01-03T16:48:00Z",
    "Authors": [
      "Alva Sundin"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "216",
    "Title": "Letters from North Wind",
    "Isbn13": "9780000002167",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2024-05-14T12:24:00Z",
    "UpdatedAtUtc": "2026-03-01T18:45:00Z",
    "Authors": [
      "Elsa Lindberg"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "217",
    "Title": "Cold Red Ledger",
    "Isbn13": "9780000002174",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 2017,
    "CreatedAtUtc": "2024-03-27T21:56:00Z",
    "UpdatedAtUtc": "2026-03-03T09:06:00Z",
    "Authors": [
      "Samuel Brooks"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "218",
    "Title": "The Missing Winter File",
    "Isbn13": "9780000002181",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2004,
    "CreatedAtUtc": "2024-06-12T08:37:00Z",
    "UpdatedAtUtc": "2026-02-08T03:30:00Z",
    "Authors": [
      "Lena Nyström",
      "Frida Nyström"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "219",
    "Title": "Practical Digital Declutter Plan",
    "Isbn13": "9780000002198",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 1994,
    "CreatedAtUtc": "2024-12-06T02:31:00Z",
    "UpdatedAtUtc": "2026-02-03T10:40:00Z",
    "Authors": [
      "Eleanor Bishop"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "220",
    "Title": "Beyond Silent Room",
    "Isbn13": "9780000002204",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2024-07-14T15:24:00Z",
    "UpdatedAtUtc": "2026-01-01T17:12:00Z",
    "Authors": [
      "Lena Bergqvist",
      "Elin Åkesson"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "221",
    "Title": "The Last Starforge",
    "Isbn13": "9780000002211",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2007,
    "CreatedAtUtc": "2024-03-04T12:32:00Z",
    "UpdatedAtUtc": "2026-03-05T05:04:00Z",
    "Authors": [
      "Thomas Foster"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "222",
    "Title": "The Last Iron Moon",
    "Isbn13": "9780000002228",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 1986,
    "CreatedAtUtc": "2025-09-06T03:23:00Z",
    "UpdatedAtUtc": "2026-03-01T12:56:00Z",
    "Authors": [
      "Anna Lindberg",
      "Ebba Nyström",
      "Elsa Sandberg"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "223",
    "Title": "Chronicles of Frost Signal",
    "Isbn13": "9780000002235",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 1992,
    "CreatedAtUtc": "2025-05-16T22:09:00Z",
    "UpdatedAtUtc": "2026-01-01T09:26:00Z",
    "Authors": [
      "Maja Åkesson",
      "Lovisa Wallin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "224",
    "Title": "Letters from Silent Room",
    "Isbn13": "9780000002242",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 2017,
    "CreatedAtUtc": "2025-10-04T09:07:00Z",
    "UpdatedAtUtc": "2026-01-08T06:38:00Z",
    "Authors": [
      "Jonathan Ellis"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "225",
    "Title": "The Last Ash Kingdom",
    "Isbn13": "9780000002259",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2025-08-07T22:59:00Z",
    "UpdatedAtUtc": "2026-02-06T07:00:00Z",
    "Authors": [
      "Linnea Nordin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "226",
    "Title": "A Tiny Balcony Garden",
    "Isbn13": "9780000002266",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 1991,
    "CreatedAtUtc": "2023-01-28T09:23:00Z",
    "UpdatedAtUtc": "2026-03-06T13:47:00Z",
    "Authors": [
      "Ebba Engström",
      "Linnea Lundqvist",
      "Lovisa Sjöström"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "227",
    "Title": "The Winter",
    "Isbn13": "9780000002273",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2024-12-03T11:52:00Z",
    "UpdatedAtUtc": "2026-02-03T03:25:00Z",
    "Authors": [
      "Ingrid Sundin",
      "Frida Engström"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "228",
    "Title": "Beyond Northern Reform",
    "Isbn13": "9780000002280",
    "Language": "English",
    "Publisher": "Orbit",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2025-08-06T19:24:00Z",
    "UpdatedAtUtc": "2026-02-10T00:55:00Z",
    "Authors": [
      "Michael Spencer",
      "Eleanor Parker"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "229",
    "Title": "A Nordic Woodshop",
    "Isbn13": "9780000002297",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2023-12-28T21:33:00Z",
    "UpdatedAtUtc": "2026-03-10T23:36:00Z",
    "Authors": [
      "Lena Lundqvist"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "230",
    "Title": "The Complete Better Sleep Manual",
    "Isbn13": "9780000002303",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "An approachable handbook offering useful techniques, checklists, and encouragement for steady progress.",
    "PublicationYear": 2012,
    "CreatedAtUtc": "2025-12-08T15:22:00Z",
    "UpdatedAtUtc": "2026-03-07T04:53:00Z",
    "Authors": [
      "Elin Holm"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "231",
    "Title": "The Cinnamon Trail",
    "Isbn13": "9780000002310",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2024-04-11T08:03:00Z",
    "UpdatedAtUtc": "2026-03-04T17:41:00Z",
    "Authors": [
      "Nora Norberg",
      "Ingrid Ekman"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "232",
    "Title": "Lives of Cartographer's Daughter",
    "Isbn13": "9780000002327",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2024-01-18T17:00:00Z",
    "UpdatedAtUtc": "2026-01-10T19:40:00Z",
    "Authors": [
      "Maja Norberg",
      "Astrid Holm"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "233",
    "Title": "Summer at Clocktower Club",
    "Isbn13": "9780000002334",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2015,
    "CreatedAtUtc": "2025-11-12T22:28:00Z",
    "UpdatedAtUtc": "2026-01-02T21:14:00Z",
    "Authors": [
      "Emilia Öhman",
      "Elin Viklund"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "234",
    "Title": "A Night Engine",
    "Isbn13": "9780000002341",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 1995,
    "CreatedAtUtc": "2024-03-27T22:47:00Z",
    "UpdatedAtUtc": "2026-03-07T03:16:00Z",
    "Authors": [
      "Maja Sjöström"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "235",
    "Title": "A Glass Trail",
    "Isbn13": "9780000002358",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 1991,
    "CreatedAtUtc": "2024-01-21T21:37:00Z",
    "UpdatedAtUtc": "2026-01-08T14:42:00Z",
    "Authors": [
      "Sofia Sundin"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "236",
    "Title": "Adventures of Clocktower Club",
    "Isbn13": "9780000002365",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2012,
    "CreatedAtUtc": "2025-08-17T05:44:00Z",
    "UpdatedAtUtc": "2026-03-03T23:52:00Z",
    "Authors": [
      "Karin Öhman"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "237",
    "Title": "The Quiet Silent Room",
    "Isbn13": "9780000002372",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 2009,
    "CreatedAtUtc": "2024-10-23T07:17:00Z",
    "UpdatedAtUtc": "2026-02-06T09:28:00Z",
    "Authors": [
      "Isabel Griffin"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "238",
    "Title": "The Burning Thaw",
    "Isbn13": "9780000002389",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 1997,
    "CreatedAtUtc": "2023-10-23T09:43:00Z",
    "UpdatedAtUtc": "2026-02-03T22:41:00Z",
    "Authors": [
      "Tove Sundin",
      "Tove Sandberg"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "239",
    "Title": "Summer at Snowy Hill",
    "Isbn13": "9780000002396",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2015,
    "CreatedAtUtc": "2025-10-22T22:26:00Z",
    "UpdatedAtUtc": "2026-03-09T09:56:00Z",
    "Authors": [
      "Frida Öhman"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "240",
    "Title": "A Winter",
    "Isbn13": "9780000002402",
    "Language": "Norwegian",
    "Publisher": "HarperCollins",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2023-05-22T08:09:00Z",
    "UpdatedAtUtc": "2026-01-05T06:48:00Z",
    "Authors": [
      "Karin Sandberg"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "241",
    "Title": "Before Harbor",
    "Isbn13": "9780000002419",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 2020,
    "CreatedAtUtc": "2024-04-17T22:18:00Z",
    "UpdatedAtUtc": "2026-03-01T12:24:00Z",
    "Authors": [
      "Tove Nyström"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "242",
    "Title": "Everyday Digital Declutter Plan",
    "Isbn13": "9780000002426",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2025-12-21T11:48:00Z",
    "UpdatedAtUtc": "2026-03-09T23:38:00Z",
    "Authors": [
      "Lovisa Nyström"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "243",
    "Title": "Everyday Family Meal Planner",
    "Isbn13": "9780000002433",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 1996,
    "CreatedAtUtc": "2025-09-04T12:02:00Z",
    "UpdatedAtUtc": "2026-02-05T17:16:00Z",
    "Authors": [
      "Emily Turner"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "244",
    "Title": "Silent Witness",
    "Isbn13": "9780000002440",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 1994,
    "CreatedAtUtc": "2024-10-02T20:39:00Z",
    "UpdatedAtUtc": "2026-03-05T15:50:00Z",
    "Authors": [
      "Elsa Sjöström"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "245",
    "Title": "My Snowy Hill",
    "Isbn13": "9780000002457",
    "Language": "English",
    "Publisher": "Tor Books",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2023-09-06T13:34:00Z",
    "UpdatedAtUtc": "2026-03-07T02:22:00Z",
    "Authors": [
      "Thomas Turner"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "246",
    "Title": "Hidden Dead Letter",
    "Isbn13": "9780000002464",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2024-09-27T20:00:00Z",
    "UpdatedAtUtc": "2026-01-06T14:15:00Z",
    "Authors": [
      "Sofia Viklund"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "247",
    "Title": "The Secret of Clocktower Club",
    "Isbn13": "9780000002471",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 1992,
    "CreatedAtUtc": "2024-05-18T23:18:00Z",
    "UpdatedAtUtc": "2026-01-09T11:32:00Z",
    "Authors": [
      "Emilia Wallin"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "248",
    "Title": "The Cartographer's Daughter",
    "Isbn13": "9780000002488",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2025-02-07T21:40:00Z",
    "UpdatedAtUtc": "2026-01-07T03:13:00Z",
    "Authors": [
      "George Lawson"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "249",
    "Title": "Smart Mindful Walking Guide",
    "Isbn13": "9780000002495",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2023-02-02T05:50:00Z",
    "UpdatedAtUtc": "2026-02-09T02:36:00Z",
    "Authors": [
      "Elsa Lindberg",
      "Lovisa Sundin"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "250",
    "Title": "Children of Glass Planet",
    "Isbn13": "9780000002501",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2019,
    "CreatedAtUtc": "2023-11-24T19:04:00Z",
    "UpdatedAtUtc": "2026-03-10T01:44:00Z",
    "Authors": [
      "Anna Holm"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "251",
    "Title": "The Glass Planet",
    "Isbn13": "9780000002518",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A richly imagined fantasy in which old powers awaken and a reluctant hero must choose between duty and freedom.",
    "PublicationYear": 1995,
    "CreatedAtUtc": "2025-04-05T23:25:00Z",
    "UpdatedAtUtc": "2026-03-05T20:09:00Z",
    "Authors": [
      "Astrid Norberg"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "252",
    "Title": "A Calm Kitchen",
    "Isbn13": "9780000002525",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2011,
    "CreatedAtUtc": "2025-09-05T05:11:00Z",
    "UpdatedAtUtc": "2026-01-10T08:12:00Z",
    "Authors": [
      "Frida Sandberg"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "253",
    "Title": "The Missing Black Pier",
    "Isbn13": "9780000002532",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 1989,
    "CreatedAtUtc": "2024-02-07T23:37:00Z",
    "UpdatedAtUtc": "2026-02-06T11:08:00Z",
    "Authors": [
      "Emily Hayes"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "254",
    "Title": "A Digital Declutter Plan",
    "Isbn13": "9780000002549",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2025-05-23T18:09:00Z",
    "UpdatedAtUtc": "2026-03-10T11:51:00Z",
    "Authors": [
      "Rachel Coleman"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "255",
    "Title": "The Hidden Court",
    "Isbn13": "9780000002556",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2024-03-22T23:21:00Z",
    "UpdatedAtUtc": "2026-02-08T14:22:00Z",
    "Authors": [
      "Ebba Åkesson"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "256",
    "Title": "The Last Years of Smoke",
    "Isbn13": "9780000002563",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2023-10-19T12:49:00Z",
    "UpdatedAtUtc": "2026-03-04T01:35:00Z",
    "Authors": [
      "Emily Morgan",
      "Anna Griffin"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "257",
    "Title": "Adventures of Cinnamon Trail",
    "Isbn13": "9780000002570",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2014,
    "CreatedAtUtc": "2025-09-03T12:23:00Z",
    "UpdatedAtUtc": "2026-03-01T21:15:00Z",
    "Authors": [
      "Ebba Molin",
      "Linnea Bergqvist"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "258",
    "Title": "Practical Family Meal Planner",
    "Isbn13": "9780000002587",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 1994,
    "CreatedAtUtc": "2025-12-16T11:02:00Z",
    "UpdatedAtUtc": "2026-01-08T07:04:00Z",
    "Authors": [
      "Saga Molin"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "259",
    "Title": "The Last Night Engine",
    "Isbn13": "9780000002594",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2023-12-17T20:53:00Z",
    "UpdatedAtUtc": "2026-02-05T18:34:00Z",
    "Authors": [
      "Samuel Bishop",
      "James Carter"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "260",
    "Title": "The Burning Red Ledger",
    "Isbn13": "9780000002600",
    "Language": "Swedish",
    "Publisher": "Forum",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2024-06-16T15:56:00Z",
    "UpdatedAtUtc": "2026-03-01T16:56:00Z",
    "Authors": [
      "Alva Wallin",
      "Ebba Hedlund"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "261",
    "Title": "A Years of Smoke",
    "Isbn13": "9780000002617",
    "Language": "English",
    "Publisher": "Bloomsbury",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 1992,
    "CreatedAtUtc": "2025-04-14T10:46:00Z",
    "UpdatedAtUtc": "2026-01-08T21:30:00Z",
    "Authors": [
      "David Murray"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "262",
    "Title": "Dark Red Ledger",
    "Isbn13": "9780000002624",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 1995,
    "CreatedAtUtc": "2023-11-17T01:57:00Z",
    "UpdatedAtUtc": "2026-03-03T01:10:00Z",
    "Authors": [
      "Karin Bergqvist",
      "Saga Molin"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "263",
    "Title": "The Last Ash District",
    "Isbn13": "9780000002631",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2024-08-06T07:53:00Z",
    "UpdatedAtUtc": "2026-02-10T04:50:00Z",
    "Authors": [
      "Linnea Ekman",
      "Sofia Engström"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "264",
    "Title": "The Missing Snow Line",
    "Isbn13": "9780000002648",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2023,
    "CreatedAtUtc": "2025-02-08T12:24:00Z",
    "UpdatedAtUtc": "2026-01-02T00:12:00Z",
    "Authors": [
      "Emilia Engström"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "265",
    "Title": "My Sea Glass Summer",
    "Isbn13": "9780000002655",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2025-09-25T03:05:00Z",
    "UpdatedAtUtc": "2026-02-03T19:34:00Z",
    "Authors": [
      "Nathan Murray"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "266",
    "Title": "Smart Creative Habit Workbook",
    "Isbn13": "9780000002662",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2007,
    "CreatedAtUtc": "2025-12-25T22:36:00Z",
    "UpdatedAtUtc": "2026-01-07T04:16:00Z",
    "Authors": [
      "Sara Nyström",
      "Nora Wallin"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "267",
    "Title": "Silent Ash District",
    "Isbn13": "9780000002679",
    "Language": "English",
    "Publisher": "HarperCollins",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2018,
    "CreatedAtUtc": "2023-09-01T12:41:00Z",
    "UpdatedAtUtc": "2026-01-06T13:23:00Z",
    "Authors": [
      "Olivia Bennett",
      "Anna Wells"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "268",
    "Title": "The Burning Snow Line",
    "Isbn13": "9780000002686",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2024-10-09T21:25:00Z",
    "UpdatedAtUtc": "2026-01-07T17:50:00Z",
    "Authors": [
      "George Spencer"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "269",
    "Title": "Witness to Century of Bread",
    "Isbn13": "9780000002693",
    "Language": "Norwegian",
    "Publisher": "Natur & Kultur",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2025-03-28T21:07:00Z",
    "UpdatedAtUtc": "2026-03-04T14:50:00Z",
    "Authors": [
      "Eleanor Brooks"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "270",
    "Title": "Cold Final Alibi",
    "Isbn13": "9780000002709",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A tense investigation begins when a routine case exposes corruption, secrets, and a killer who stays one step ahead.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2025-11-23T01:10:00Z",
    "UpdatedAtUtc": "2026-02-01T02:52:00Z",
    "Authors": [
      "Emilia Öhman"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "271",
    "Title": "A Hidden Court",
    "Isbn13": "9780000002716",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2025-11-17T09:45:00Z",
    "UpdatedAtUtc": "2026-03-01T05:40:00Z",
    "Authors": [
      "Karin Wallin"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "272",
    "Title": "Witness to Years of Smoke",
    "Isbn13": "9780000002723",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2023-02-09T12:22:00Z",
    "UpdatedAtUtc": "2026-03-07T12:37:00Z",
    "Authors": [
      "Frida Nyström"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "273",
    "Title": "The Curious Midnight Notebook",
    "Isbn13": "9780000002730",
    "Language": "English",
    "Publisher": "Penguin Books",
    "Summary": "A lively children's book in which curiosity leads to mishaps, discoveries, and unexpected kindness.",
    "PublicationYear": 2010,
    "CreatedAtUtc": "2025-01-09T05:26:00Z",
    "UpdatedAtUtc": "2026-03-02T17:04:00Z",
    "Authors": [
      "Olivia Bishop"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "274",
    "Title": "Chronicles of Night Engine",
    "Isbn13": "9780000002747",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2024-04-25T10:49:00Z",
    "UpdatedAtUtc": "2026-02-08T18:55:00Z",
    "Authors": [
      "Saga Lundqvist",
      "Lovisa Wallin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "275",
    "Title": "Lives of Summer Republic",
    "Isbn13": "9780000002754",
    "Language": "Norwegian",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2007,
    "CreatedAtUtc": "2025-09-01T22:53:00Z",
    "UpdatedAtUtc": "2026-02-04T09:05:00Z",
    "Authors": [
      "Emilia Åkesson"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "276",
    "Title": "The Complete Home Budget Handbook",
    "Isbn13": "9780000002761",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2024-04-19T08:11:00Z",
    "UpdatedAtUtc": "2026-03-04T15:47:00Z",
    "Authors": [
      "Ingrid Norberg"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "277",
    "Title": "Cold Snow Line",
    "Isbn13": "9780000002778",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2024-03-25T03:07:00Z",
    "UpdatedAtUtc": "2026-01-08T19:00:00Z",
    "Authors": [
      "Sofia Nyström"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "278",
    "Title": "Before Long Road",
    "Isbn13": "9780000002785",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A reflective novel about family loyalties, hidden grief, and the choices that reshape an ordinary life.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2024-03-25T16:17:00Z",
    "UpdatedAtUtc": "2026-02-04T18:08:00Z",
    "Authors": [
      "Nora Molin"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "279",
    "Title": "Adventures of Paper Dragon",
    "Isbn13": "9780000002792",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A warm and adventurous story for younger readers about friendship, courage, and finding a place to belong.",
    "PublicationYear": 1996,
    "CreatedAtUtc": "2024-02-23T16:29:00Z",
    "UpdatedAtUtc": "2026-02-01T00:43:00Z",
    "Authors": [
      "David Ward"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "280",
    "Title": "Portrait of Quiet Resistance",
    "Isbn13": "9780000002808",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A biography that follows an influential life through private letters, public controversy, and lasting legacy.",
    "PublicationYear": 2024,
    "CreatedAtUtc": "2024-06-06T15:27:00Z",
    "UpdatedAtUtc": "2026-01-07T22:48:00Z",
    "Authors": [
      "Alva Nyström",
      "Alva Norberg"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "281",
    "Title": "Legends of Hidden Orbit",
    "Isbn13": "9780000002815",
    "Language": "English",
    "Publisher": "Faber & Faber",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 2005,
    "CreatedAtUtc": "2024-05-09T17:54:00Z",
    "UpdatedAtUtc": "2026-03-01T12:06:00Z",
    "Authors": [
      "Samuel Reed",
      "Amelia Lawson"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "282",
    "Title": "The Seventh Red Ledger",
    "Isbn13": "9780000002822",
    "Language": "Norwegian",
    "Publisher": "Norstedts",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2008,
    "CreatedAtUtc": "2025-09-26T08:22:00Z",
    "UpdatedAtUtc": "2026-01-07T03:22:00Z",
    "Authors": [
      "Hannah Brooks"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "283",
    "Title": "A Blue Bicycle",
    "Isbn13": "9780000002839",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2013,
    "CreatedAtUtc": "2023-07-20T09:29:00Z",
    "UpdatedAtUtc": "2026-03-03T05:40:00Z",
    "Authors": [
      "Matilda Norberg"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "284",
    "Title": "Beyond Engineer of Light",
    "Isbn13": "9780000002846",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 2002,
    "CreatedAtUtc": "2024-06-24T18:33:00Z",
    "UpdatedAtUtc": "2026-02-07T05:11:00Z",
    "Authors": [
      "Elin Wallin"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "285",
    "Title": "A Night Caller",
    "Isbn13": "9780000002853",
    "Language": "English",
    "Publisher": "Vintage",
    "Summary": "A fast-moving crime novel where a determined investigator follows a trail of lies through a city under pressure.",
    "PublicationYear": 1989,
    "CreatedAtUtc": "2024-09-02T00:49:00Z",
    "UpdatedAtUtc": "2026-02-09T02:06:00Z",
    "Authors": [
      "Michael Ellis"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "286",
    "Title": "My Cinnamon Trail",
    "Isbn13": "9780000002860",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A YA novel about first independence, shifting friendships, and the brave choices that come with growing up.",
    "PublicationYear": 2025,
    "CreatedAtUtc": "2023-03-17T05:01:00Z",
    "UpdatedAtUtc": "2026-01-06T10:55:00Z",
    "Authors": [
      "Ebba Nyström",
      "Linnea Engström"
    ],
    "CategoryHint": "children / YA"
  },
  {
    "Id": "287",
    "Title": "Beyond Quiet Resistance",
    "Isbn13": "9780000002877",
    "Language": "Swedish",
    "Publisher": "Bokförlaget Polaris",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 2007,
    "CreatedAtUtc": "2024-09-08T02:28:00Z",
    "UpdatedAtUtc": "2026-02-06T13:16:00Z",
    "Authors": [
      "Tove Dahl"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "288",
    "Title": "The Night Engine",
    "Isbn13": "9780000002884",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A speculative adventure about a fragile alliance, forbidden knowledge, and a world changing faster than its people can bear.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2025-08-25T15:19:00Z",
    "UpdatedAtUtc": "2026-01-07T09:38:00Z",
    "Authors": [
      "Frida Dahl",
      "Nora Öhman"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "289",
    "Title": "After City Shore",
    "Isbn13": "9780000002891",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 2009,
    "CreatedAtUtc": "2024-04-09T02:27:00Z",
    "UpdatedAtUtc": "2026-03-04T16:48:00Z",
    "Authors": [
      "Ebba Nordin"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "290",
    "Title": "Everyday Calm Kitchen",
    "Isbn13": "9780000002907",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A concise nonfiction book that turns complex habits into manageable routines for everyday life.",
    "PublicationYear": 2011,
    "CreatedAtUtc": "2023-02-06T04:18:00Z",
    "UpdatedAtUtc": "2026-01-08T19:28:00Z",
    "Authors": [
      "Tove Lundqvist"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "291",
    "Title": "The Modern Creative Habit Workbook",
    "Isbn13": "9780000002914",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "A practical guide with clear advice, realistic examples, and step-by-step methods readers can apply immediately.",
    "PublicationYear": 1986,
    "CreatedAtUtc": "2023-02-10T08:08:00Z",
    "UpdatedAtUtc": "2026-03-08T23:00:00Z",
    "Authors": [
      "James Lawson"
    ],
    "CategoryHint": "practical nonfiction"
  },
  {
    "Id": "292",
    "Title": "The Midsummer Light",
    "Isbn13": "9780000002921",
    "Language": "Swedish",
    "Publisher": "Albert Bonniers Förlag",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 2016,
    "CreatedAtUtc": "2024-08-23T01:21:00Z",
    "UpdatedAtUtc": "2026-01-03T01:17:00Z",
    "Authors": [
      "Sara Dahl"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "293",
    "Title": "Between North Wind",
    "Isbn13": "9780000002938",
    "Language": "English",
    "Publisher": "Simon & Schuster",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 1987,
    "CreatedAtUtc": "2023-08-24T03:33:00Z",
    "UpdatedAtUtc": "2026-03-04T15:24:00Z",
    "Authors": [
      "Thomas Ward",
      "Rachel Foster"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "294",
    "Title": "Echoes of Starforge",
    "Isbn13": "9780000002945",
    "Language": "Swedish",
    "Publisher": "Natur & Kultur",
    "Summary": "A science-fiction novel about survival, discovery, and the moral compromises required to protect a future colony.",
    "PublicationYear": 1985,
    "CreatedAtUtc": "2023-05-19T03:25:00Z",
    "UpdatedAtUtc": "2026-01-01T01:37:00Z",
    "Authors": [
      "Maja Nyström",
      "Sara Sundin"
    ],
    "CategoryHint": "fantasy / sci-fi"
  },
  {
    "Id": "295",
    "Title": "A Century of Bread",
    "Isbn13": "9780000002952",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "A vivid work of narrative history tracing personal ambition through a period of political and social upheaval.",
    "PublicationYear": 2013,
    "CreatedAtUtc": "2025-04-01T22:24:00Z",
    "UpdatedAtUtc": "2026-02-03T08:57:00Z",
    "Authors": [
      "Tove Björk"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "296",
    "Title": "Lives of Mayor's Notebook",
    "Isbn13": "9780000002969",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "An accessible history that connects individual stories to wider changes in society, labor, and culture.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2023-09-12T10:46:00Z",
    "UpdatedAtUtc": "2026-02-05T19:58:00Z",
    "Authors": [
      "Nora Sandberg"
    ],
    "CategoryHint": "history / biography"
  },
  {
    "Id": "297",
    "Title": "Silent Final Alibi",
    "Isbn13": "9780000002976",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2009,
    "CreatedAtUtc": "2025-03-01T19:54:00Z",
    "UpdatedAtUtc": "2026-02-10T07:20:00Z",
    "Authors": [
      "Astrid Hedlund"
    ],
    "CategoryHint": "crime / thriller"
  },
  {
    "Id": "298",
    "Title": "Songs of North Wind",
    "Isbn13": "9780000002983",
    "Language": "Swedish",
    "Publisher": "Ordfront",
    "Summary": "An intimate literary novel in which a return home uncovers old promises and unresolved loss.",
    "PublicationYear": 1994,
    "CreatedAtUtc": "2025-05-11T13:41:00Z",
    "UpdatedAtUtc": "2026-02-02T03:33:00Z",
    "Authors": [
      "Nora Öhman"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "299",
    "Title": "Beyond Birches",
    "Isbn13": "9780000002990",
    "Language": "Swedish",
    "Publisher": "Norstedts",
    "Summary": "A character-driven story about memory, belonging, and the fragile ties between parents and children.",
    "PublicationYear": 2021,
    "CreatedAtUtc": "2025-07-22T15:33:00Z",
    "UpdatedAtUtc": "2026-03-10T08:03:00Z",
    "Authors": [
      "Nora Sandberg",
      "Maja Sjöström"
    ],
    "CategoryHint": "literary fiction"
  },
  {
    "Id": "300",
    "Title": "A Witness",
    "Isbn13": "9780000003003",
    "Language": "Swedish",
    "Publisher": "Wahlström & Widstrand",
    "Summary": "A psychological thriller about disappearance, obsession, and the dangerous cost of getting too close to the truth.",
    "PublicationYear": 2008,
    "CreatedAtUtc": "2025-12-14T11:50:00Z",
    "UpdatedAtUtc": "2026-03-01T19:17:00Z",
    "Authors": [
      "Nora Ekman",
      "Lovisa Sjöström"
    ],
    "CategoryHint": "crime / thriller"
  }
]
""";
    }
}

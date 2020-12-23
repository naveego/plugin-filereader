using System.Collections.Generic;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Factory.Implementations.AS400
{
    public static class AS400
    {
        public static List<AS400Format> Format25 = new List<AS400Format>
        {
            new AS400Format
            {
                SingleRecordPerLine = true,
                IsGlobalHeader = true,
                KeyValue = new AS400KeyValue
                {
                    Value = "10",
                },
                Columns = new List<Column>
                {
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Company",
                        ColumnStart = 2,
                        ColumnEnd = 3
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Number",
                        ColumnStart = 4,
                        ColumnEnd = 19
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Company Name",
                        ColumnStart = 20,
                        ColumnEnd = 59
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Transaction Date",
                        ColumnStart = 60,
                        ColumnEnd = 66
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Renewal Policy Number",
                        ColumnStart = 67,
                        ColumnEnd = 3
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Number",
                        ColumnStart = 83,
                        ColumnEnd = 88
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Name",
                        ColumnStart = 89,
                        ColumnEnd = 128
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Address Line 1",
                        ColumnStart = 129,
                        ColumnEnd = 168
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Address Line 2",
                        ColumnStart = 169,
                        ColumnEnd = 208
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent City",
                        ColumnStart = 209,
                        ColumnEnd = 238
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent State",
                        ColumnStart = 239,
                        ColumnEnd = 240
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Zip Code",
                        ColumnStart = 241,
                        ColumnEnd = 249
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Commission",
                        ColumnStart = 250,
                        ColumnEnd = 252
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Applies Code",
                        ColumnStart = 253,
                        ColumnEnd = 253
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Audit Code",
                        ColumnStart = 254,
                        ColumnEnd = 254
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "New York Free Trade Zone",
                        ColumnStart = 255,
                        ColumnEnd = 255
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "A Rate Company",
                        ColumnStart = 256,
                        ColumnEnd = 256
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Currency Code",
                        ColumnStart = 257,
                        ColumnEnd = 259
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Branch Code",
                        ColumnStart = 260,
                        ColumnEnd = 261
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Effective Date",
                        ColumnStart = 262,
                        ColumnEnd = 269
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Expiration Date",
                        ColumnStart = 270,
                        ColumnEnd = 277
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Product Code",
                        ColumnStart = 278,
                        ColumnEnd = 280
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Aggregate",
                        ColumnStart = 281,
                        ColumnEnd = 288
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Excess Policy Indicator",
                        ColumnStart = 289,
                        ColumnEnd = 289
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Prime Carrier Name",
                        ColumnStart = 290,
                        ColumnEnd = 329
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Prime Carrier Limits",
                        ColumnStart = 330,
                        ColumnEnd = 337
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Form Type",
                        ColumnStart = 338,
                        ColumnEnd = 338
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Transaction",
                        ColumnStart = 339,
                        ColumnEnd = 339
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Cancellation Date",
                        ColumnStart = 340,
                        ColumnEnd = 346
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Parent Company",
                        ColumnStart = 347,
                        ColumnEnd = 348
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "System Code",
                        ColumnStart = 349,
                        ColumnEnd = 349
                    },
                }
            },
            new AS400Format
            {
                SingleRecordPerLine = true,
                IsGlobalHeader = true,
                KeyValue = new AS400KeyValue
                {
                    Value = "11",
                },
                Columns = new List<Column>
                {
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Endorsement Number",
                        ColumnStart = 2,
                        ColumnEnd = 5
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Entity",
                        ColumnStart = 6,
                        ColumnEnd = 6
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Name",
                        ColumnStart = 7,
                        ColumnEnd = 46
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Address Line 1",
                        ColumnStart = 47,
                        ColumnEnd = 86
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Address Line 2",
                        ColumnStart = 87,
                        ColumnEnd = 126
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account City",
                        ColumnStart = 127,
                        ColumnEnd = 156
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account State",
                        ColumnStart = 157,
                        ColumnEnd = 158
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Zip Code",
                        ColumnStart = 159,
                        ColumnEnd = 167
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "DBA Name",
                        ColumnStart = 168,
                        ColumnEnd = 212
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Number",
                        ColumnStart = 213,
                        ColumnEnd = 221
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "New Renewal Dec",
                        ColumnStart = 222,
                        ColumnEnd = 222
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Business Description",
                        ColumnStart = 223,
                        ColumnEnd = 291
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy State",
                        ColumnStart = 292,
                        ColumnEnd = 293
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Total Policy Premium",
                        ColumnStart = 294,
                        ColumnEnd = 318
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Group Profile",
                        ColumnStart = 319,
                        ColumnEnd = 328
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "User Profile",
                        ColumnStart = 329,
                        ColumnEnd = 338
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Current Date",
                        ColumnStart = 339,
                        ColumnEnd = 345
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Bill Code",
                        ColumnStart = 346,
                        ColumnEnd = 347
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Reporting Period",
                        ColumnStart = 348,
                        ColumnEnd = 348
                    },
                }
            },
            new AS400Format
            {
                SingleRecordPerLine = true,
                IsGlobalHeader = true,
                KeyValue = new AS400KeyValue
                {
                    Value = "12",
                },
                Columns = new List<Column>
                {
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Language",
                        ColumnStart = 2,
                        ColumnEnd = 11
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Transaction Effective Date Edited",
                        ColumnStart = 12,
                        ColumnEnd = 19
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Transaction Effective Date Numeric",
                        ColumnStart = 20,
                        ColumnEnd = 26
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Authorized Representative Name",
                        ColumnStart = 27,
                        ColumnEnd = 66
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Auth Rep Address Line 1",
                        ColumnStart = 67,
                        ColumnEnd = 106
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Auth Rep Address Line 2",
                        ColumnStart = 107,
                        ColumnEnd = 146
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Auth Rep City",
                        ColumnStart = 147,
                        ColumnEnd = 171
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Auth Rep State",
                        ColumnStart = 172,
                        ColumnEnd = 173
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Auth Rep Zip Code",
                        ColumnStart = 174,
                        ColumnEnd = 182
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Surplus Lines Name",
                        ColumnStart = 183,
                        ColumnEnd = 222
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Surplus Lines Company",
                        ColumnStart = 223,
                        ColumnEnd = 262
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Surplus Lines Address Line 1",
                        ColumnStart = 263,
                        ColumnEnd = 302
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Surplus Lines Address Line 2",
                        ColumnStart = 303,
                        ColumnEnd = 342
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "WAIVCOL Coverage Codes Counter",
                        ColumnStart = 343,
                        ColumnEnd = 345
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "CGLB Line of Business Counter",
                        ColumnStart = 346,
                        ColumnEnd = 348
                    },
                }
            },
            new AS400Format
            {
                SingleRecordPerLine = true,
                IsGlobalHeader = true,
                KeyValue = new AS400KeyValue
                {
                    Value = "13",
                },
                Columns = new List<Column>
                {
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Surplus Lines City",
                        ColumnStart = 2,
                        ColumnEnd = 41
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Countersigning Name",
                        ColumnStart = 42,
                        ColumnEnd = 81
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Countersigning Company",
                        ColumnStart = 82,
                        ColumnEnd = 121
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Countersign Address Line 1",
                        ColumnStart = 122,
                        ColumnEnd = 161
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Countersign Address Line 2",
                        ColumnStart = 162,
                        ColumnEnd = 201
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Countersign City",
                        ColumnStart = 202,
                        ColumnEnd = 241
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Executive Name",
                        ColumnStart = 242,
                        ColumnEnd = 281
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Executive Title",
                        ColumnStart = 282,
                        ColumnEnd = 321
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Effective Time",
                        ColumnStart = 322,
                        ColumnEnd = 326
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "TUX Switch",
                        ColumnStart = 327,
                        ColumnEnd = 327
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Blanket Policy",
                        ColumnStart = 328,
                        ColumnEnd = 328
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "New Renewal Verbiage",
                        ColumnStart = 329,
                        ColumnEnd = 343
                    },
                }
            },
            new AS400Format
            {
                SingleRecordPerLine = true,
                IsGlobalHeader = true,
                KeyValue = new AS400KeyValue
                {
                    Value = "14",
                },
                Columns = new List<Column>
                {
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Division Name",
                        ColumnStart = 2,
                        ColumnEnd = 41
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Surplus Lines Applicable",
                        ColumnStart = 42,
                        ColumnEnd = 42
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Countersigning Agent Applicable 14",
                        ColumnStart = 43,
                        ColumnEnd = 43
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Miscellaneous Field 1",
                        ColumnStart = 44,
                        ColumnEnd = 53
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Miscellaneous Field 2",
                        ColumnStart = 54,
                        ColumnEnd = 63
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Address Line 3",
                        ColumnStart = 64,
                        ColumnEnd = 103
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Account Address Line 4",
                        ColumnStart = 104,
                        ColumnEnd = 143
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Address Line 3",
                        ColumnStart = 144,
                        ColumnEnd = 183
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Number of Copies Other",
                        ColumnStart = 184,
                        ColumnEnd = 185
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Number of Copies Mortgagor",
                        ColumnStart = 186,
                        ColumnEnd = 187
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Number of Copies Loss Payee",
                        ColumnStart = 188,
                        ColumnEnd = 189
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Transaction Date",
                        ColumnStart = 190,
                        ColumnEnd = 197
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "User Name",
                        ColumnStart = 198,
                        ColumnEnd = 227
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Product Description",
                        ColumnStart = 228,
                        ColumnEnd = 267
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Effective Date Long",
                        ColumnStart = 268,
                        ColumnEnd = 285
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Expiration Date Long",
                        ColumnStart = 286,
                        ColumnEnd = 303
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Transaction Effective Date",
                        ColumnStart = 304,
                        ColumnEnd = 321
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Policy Transaction Date Long",
                        ColumnStart = 322,
                        ColumnEnd = 339
                    },
                }
            },
            new AS400Format
            {
                SingleRecordPerLine = true,
                IsGlobalHeader = true,
                KeyValue = new AS400KeyValue
                {
                    Value = "15",
                },
                Columns = new List<Column>
                {
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Currency Description",
                        ColumnStart = 2,
                        ColumnEnd = 21
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Current Date Long",
                        ColumnStart = 22,
                        ColumnEnd = 39
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Countersigning Agent Applicable 15",
                        ColumnStart = 40,
                        ColumnEnd = 41
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Cabinet Name",
                        ColumnStart = 41,
                        ColumnEnd = 50
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "GCE",
                        ColumnStart = 51,
                        ColumnEnd = 54
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Agent Code",
                        ColumnStart = 55,
                        ColumnEnd = 57
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Client Number",
                        ColumnStart = 58,
                        ColumnEnd = 67
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Submission Number",
                        ColumnStart = 68,
                        ColumnEnd = 77
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Status",
                        ColumnStart = 78,
                        ColumnEnd = 78
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Nbr of Submissions",
                        ColumnStart = 79,
                        ColumnEnd = 80
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Original Policy Tran Date",
                        ColumnStart = 81,
                        ColumnEnd = 87
                    },
                    new Column
                    {
                        IsKey = false,
                        TrimWhitespace = true,
                        IsGlobalHeader = true,
                        ColumnName = "Email Indicator",
                        ColumnStart = 88,
                        ColumnEnd = 88
                    },
                }
            },
            new AS400Format
            {
                SingleRecordPerLine = false,
                KeyValue = new AS400KeyValue
                {
                    Value = "25",
                    Name = "VEH_POL"
                },
                MultiLineDefinition = new AS400MultiLineDefinition
                {
                    TagNameStart = 2,
                    TagNameEnd = 33,
                    TagNameDelimiter = '.',
                    ValueLengthStart = 34,
                    ValueLengthEnd = 38,
                    ValueStart = 39
                },
                HeaderRecordKeys = new List<string>
                {
                    "VEH",
                    "ID"
                },
                MultiLineColumns = new List<Column>
                {
                    new Column
                    {
                        ColumnName = "VEH.NUM",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = true
                    },
                    new Column
                    {
                        ColumnName = "VEH.YR",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.MK",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.MD",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.CC",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.DRVR.DOB",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.VIN",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.TERR",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.TOWN",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.STATE",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "VEH.CLASS",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "ID.YEAR",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "ID.MAKE",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "ID.MODEL",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "ID.VIN",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "ID.STATE",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "ID.EFFECTIVE",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "ID.EXPIRE",
                        IsHeader = true,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "COVCODE",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = true
                    },
                    new Column
                    {
                        ColumnName = "PREM",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "LIMIT",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "AGG.LIMIT",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "DED",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "MEP",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "COIN",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "CLASS",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "TERR",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "SCHMOD",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "EFF.DT",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "EXP.DT",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                    new Column
                    {
                        ColumnName = "DESC",
                        IsHeader = false,
                        TrimWhitespace = true,
                        IsKey = false
                    },
                }
            },
        };
    }
}
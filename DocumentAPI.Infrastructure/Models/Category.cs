﻿using System.Collections.Generic;
using System.Linq;

namespace DocumentAPI.Infrastructure.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public IList<Attribute> Attributes { get; set; }
    }

    public class Attribute
    {
        public int FieldNumber { get; set; }

        public string Name { get; set; }

        public string FilterValue1 { get; set; }

        public string FilterValue2 { get; set; }

        public Type Type { get; set; }

        public FilterType SelectedFilterType { get; set; }
    }

    public class Type
    {
        public string Name { get; set; }
        public IList<FilterType> FilterTypes { get; set; }
    }


    public class FilterType
    {
        public string Name { get; set; }
    }

    public static class DocumentCategories
    {
        static DocumentCategories()
        {
            var numericType = new Type
            {
                Name = NumericTypeName,
                FilterTypes = new List<FilterType>
                {
                    new FilterType {Name = NumericEqualsOperator},
                    new FilterType {Name = NumericGreaterThanOperator},
                    new FilterType {Name = NumericLessThanOperator},
                    new FilterType {Name = NumericBetweenOperator}
                }
            };

            var dateType = new Type
            {
                Name = DateTypeName,
                FilterTypes = new List<FilterType>
                {
                    new FilterType{Name = DateEqualsOperator},
                    new FilterType{Name = DateGreaterThanOperator},
                    new FilterType{Name = DateLessThanOperator},
                    new FilterType{Name = DateBetweenOperator}
                }
            };

            var textType = new Type
            {
                Name = TextTypeName,
                FilterTypes = new List<FilterType>
                {
                    new FilterType{Name = TextEqualsOperator},
                    new FilterType{Name = TextGreaterThanOperator},
                    new FilterType{Name = TextLessThanOperator},
                    new FilterType{Name = TextBetweenOperator}
                }
            };

            var fullTextType = new Type
            {
                Name = FullTextSearchName,
                FilterTypes = new List<FilterType>
                {
                    new FilterType{Name = FullTextSearchName},
                }
            };

            AttributeTypes = new List<Type>
            {
                numericType, dateType, textType, fullTextType
            };

            Categories = new List<Category>
            {
                new Category
                {
                    Id = 6,
                    Name= "HISTORICAL_COMM-CARD_CATALOG",
                    DisplayName = "Card Catalog",
                    Attributes = new List<Attribute>
                    {
                        new Attribute
                        {
                            FieldNumber = 1,
                            Name = "HOUSE NUMBER",
                            Type = numericType
                        },
                        new Attribute
                        {
                            FieldNumber = 2,
                            Name = "STREET DIRECTION",
                            Type = textType
                        },
                        new Attribute
                        {
                            FieldNumber = 3,
                            Name = "STREET NAME",
                            Type = textType
                        },
                        new Attribute
                        {
                            FieldNumber = 4,
                            Name = "STREET DESIGNATION",
                            Type = textType
                        },
                        new Attribute
                        {
                            FieldNumber = 5,
                            Name = "SCAN DATE",
                            Type = dateType
                        },
                        new Attribute
                        {
                            FieldNumber = 6,
                            Name = "BOX #",
                            Type = numericType
                        }
                    }
                },
                new Category
                {
                    Id = 7,
                    Name= "HISTORICAL_COMM-MEETING_MINUTES",
                    DisplayName = "Meeting Minutes",
                    Attributes = new List<Attribute>
                    {
                        new Attribute
                        {
                            FieldNumber = 1,
                            Name = "MEETING DATE",
                            Type = dateType
                        },
                        new Attribute
                        {
                            FieldNumber = 2,
                            Name = "MEETING DESIGNATION",
                            Type = numericType
                        },
                        new Attribute
                        {
                            FieldNumber = 3,
                            Name = "SCAN DATE",
                            Type = dateType
                        },
                        new Attribute
                        {
                            FieldNumber = 4,
                            Name = "BOX #",
                            Type = numericType
                        }
                    }
                },
                new Category
                {
                    Id = 4,
                    Name= "HISTORICAL_COMM-PERMITS",
                    DisplayName = "Permits",
                    Attributes = new List<Attribute>
                    {
                        new Attribute
                        {
                            FieldNumber = 1,
                            Name = "PERMIT NUMBER",
                            Type = numericType
                        },
                        new Attribute
                        {
                            FieldNumber = 2,
                            Name = "HOUSE NUMBER",
                            Type = numericType
                        },
                        new Attribute
                        {
                            FieldNumber = 3,
                            Name = "STREET DIRECTION",
                            Type = textType
                        },
                        new Attribute
                        {
                            FieldNumber = 4,
                            Name = "STREET NAME",
                            Type = textType
                        },
                        new Attribute
                        {
                            FieldNumber = 5,
                            Name = "STREET DESIGNATION",
                            Type = textType
                        },
                        new Attribute
                        {
                            FieldNumber = 6,
                            Name = "LOCATION",
                            Type = textType
                        },
                        new Attribute
                        {
                            FieldNumber = 7,
                            Name = "SCAN DATE",
                            Type = dateType
                        },
                        new Attribute
                        {
                            FieldNumber = 8,
                            Name = "BOX #",
                            Type = numericType
                        }
                    }
                },
                new Category
                {
                    Id = 3,
                    Name= "HISTORICAL_COMM-POLAROIDS",
                    DisplayName = "Polaroids",
                    Attributes = new List<Attribute>
                    {
                        new Attribute
                        {
                            FieldNumber = 1,
                            Name = "LOCATION",
                            Type = numericType
                        },
                        new Attribute
                        {
                            FieldNumber = 2,
                            Name = "SCAN DATE",
                            Type = dateType
                        },
                        new Attribute
                        {
                            FieldNumber = 3,
                            Name = "BOX #",
                            Type = numericType
                        }
                    }
                },
                new Category
                {
                    Id = 5,
                    Name= "HISTORICAL_COMM-REGISTRY",
                    DisplayName = "Registry",
                    Attributes = new List<Attribute>
                    {
                        new Attribute
                        {
                            FieldNumber = 1,
                            Name = "LOCATION",
                            Type = textType
                        },
                        new Attribute
                        {
                            FieldNumber = 2,
                            Name = "SCAN DATE",
                            Type = dateType
                        },
                        new Attribute
                        {
                            FieldNumber = 3,
                            Name = "BOX #",
                            Type = numericType
                        }
                    }
                }
            };

            // Add full text search attribute to all Categories
            Categories.ForEach(i => i.Attributes.Add(new Attribute
            {
                FieldNumber = i.Attributes.Select(a => a.FieldNumber).Max() + 1,
                Name = FullTextSearchName,
                Type = fullTextType
            }));
        }

        public static List<Type> AttributeTypes { get; }

        public const string NumericTypeName = "NUMERIC";

        public const string NumericEqualsOperator = "EQUALS";

        public const string NumericGreaterThanOperator = "GREATER_THAN";

        public const string NumericLessThanOperator = "LESS_THAN";

        public const string NumericBetweenOperator = "BETWEEN";

        public const string DateEqualsOperator = "EQUALS";

        public const string DateGreaterThanOperator = "AFTER";

        public const string DateLessThanOperator = "BEFORE";

        public const string DateBetweenOperator = "BETWEEN";

        public const string TextEqualsOperator = "EQUALS";

        public const string TextGreaterThanOperator = "STARTS_WITH";

        public const string TextLessThanOperator = "ENDS_WITH";

        public const string TextBetweenOperator = "CONTAINS";

        public const string DateTypeName = "DATE";

        public const string TextTypeName = "TEXT";

        public const string FullTextSearchName = "FULL_TEXT";

        public static List<Category> Categories { get; }
    }
}

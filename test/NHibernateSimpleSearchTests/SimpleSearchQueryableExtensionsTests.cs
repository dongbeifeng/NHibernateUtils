﻿using System.Linq.Expressions;
using Xunit;

namespace NHibernateSimpleSearch.Tests;

public class SimpleSearchQueryableExtensionsTests
{
    #region nested classes

    class Student
    {
        public string StudentName { get; init; } = default!;

        public Clazz Clazz { get; init; } = default!;

        public int No { get; init; }
    }

    class Clazz
    {
        public string ClazzName { get; init; } = default!;
    }


    class AutoArgs
    {
        [SearchArg]
        public string? StudentName { get; set; }

        [SearchArg]
        public int? No { get; set; }
    }

    class EqualArgs
    {
        [SearchArg(SearchMode.Equal)]
        public string? StudentName { get; set; }

        [SearchArg(SearchMode.Equal)]
        public int? No { get; set; }
    }

    class NotEqualArgs
    {
        [SearchArg(SearchMode.NotEqual)]
        public int? No { get; set; }
    }

    class LikeArgs
    {
        [SearchArg(SearchMode.Like)]
        public string? StudentName { get; set; }


        [SearchArg("Clazz.ClazzName", SearchMode.Like)]
        public string? ClazzName { get; set; }
    }

    class NotLikeArgs
    {
        [SearchArg(SearchMode.NotLike)]
        public string? StudentName { get; set; }


        [SearchArg("Clazz.ClazzName", SearchMode.NotLike)]
        public string? ClazzName { get; set; }
    }

    class GreaterThanArgs
    {
        [SearchArg(SearchMode.GreaterThan)]
        public int? No { get; set; }
    }

    class GreaterThanOrEqualArgs
    {
        [SearchArg(SearchMode.GreaterThanOrEqual)]
        public int? No { get; set; }
    }

    class LessThanArgs
    {
        [SearchArg(SearchMode.LessThan)]
        public int? No { get; set; }
    }

    class LessThanOrEqualArgs
    {
        [SearchArg(SearchMode.LessThanOrEqual)]
        public int? No { get; set; }
    }

    class InArgs
    {
        [SearchArg(nameof(Student.StudentName), SearchMode.In)]
        public string?[]? StudentNames { get; set; }
    }

    class NotInArgs
    {
        [SearchArg(nameof(Student.StudentName), SearchMode.NotIn)]
        public string[]? StudentNames { get; set; }
    }

    class ExpressionArgs
    {
        [SearchArg(SearchMode.Expression)]
        public string? StudentName { get; set; }

        internal Expression<Func<Student, bool>>? StudentNameExpr
        {
            get
            {
                if (StudentName == null)
                {
                    return null;
                }
                return x => x.StudentName.Contains(StudentName);
            }
        }
    }

    class Expression2Args
    {
        [SearchArg(SearchMode.Expression)]
        public bool? ShowInvisibleClazz { get; set; }

        internal Expression<Func<Student, bool>>? ShowInvisibleClazzExpr
        {
            get
            {
                if (ShowInvisibleClazz == true)
                {
                    return null;
                }
                return x => x.Clazz.ClazzName != "invisible";
            }
        }
    }

    class FilterArgs
    {
        public IQueryable<Student> Filter(IQueryable<Student> q)
        {
            return q.Where(x => x.No == 2);
        }
    }


    #endregion

    [Fact]
    public void TestAuto()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();
        AutoArgs args1 = new AutoArgs
        {
            StudentName = "   ",
            No = 2,
        };
        AutoArgs args2 = new AutoArgs
        {
            StudentName = " Dog   ",
        };
        AutoArgs args3 = new AutoArgs
        {
            StudentName = " Do  ",
            No = 2,
        };

        var list1 = q.Filter(args1).ToList();
        var list2 = q.Filter(args2).ToList();
        var list3 = q.Filter(args3).ToList();

        Assert.Single(list1);
        Assert.Equal("Dog", list1[0].StudentName);
        Assert.Single(list2);
        Assert.Equal("Dog", list2[0].StudentName);
        Assert.Single(list3);
        Assert.Equal("Dog", list3[0].StudentName);
    }


    [Fact]
    public void TestEqual()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();
        EqualArgs args1 = new EqualArgs
        {
            StudentName = "   ",
            No = 2,
        };
        EqualArgs args2 = new EqualArgs
        {
            StudentName = " Dog   ",
        };
        EqualArgs args3 = new EqualArgs
        {
            StudentName = " Dog  ",
            No = 2,
        };

        var list1 = q.Filter(args1).ToList();
        var list2 = q.Filter(args2).ToList();
        var list3 = q.Filter(args3).ToList();

        Assert.Single(list1);
        Assert.Equal("Dog", list1[0].StudentName);
        Assert.Single(list2);
        Assert.Equal("Dog", list2[0].StudentName);
        Assert.Single(list3);
        Assert.Equal("Dog", list3[0].StudentName);
    }


    [Fact]
    public void TestNotEqual()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();
        NotEqualArgs args1 = new NotEqualArgs
        {
            No = 2,
        };

        var list1 = q.Filter(args1).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Cat", list1[1].StudentName);
    }

    [Fact]
    public void TestLike()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        {
            LikeArgs args1 = new LikeArgs
            {
                StudentName = "*o*   ",
            };

            var list1 = q.Filter(args1).ToList();

            Assert.Equal(2, list1.Count);
            Assert.Equal("Fox", list1[0].StudentName);
            Assert.Equal("Dog", list1[1].StudentName);
        }

        {
            LikeArgs args1 = new LikeArgs
            {
                StudentName = "o   ",
            };

            var list1 = q.Filter(args1).ToList();

            Assert.Equal(2, list1.Count);
            Assert.Equal("Fox", list1[0].StudentName);
            Assert.Equal("Dog", list1[1].StudentName);
        }

        {
            LikeArgs args1 = new LikeArgs
            {
                StudentName = "  *x   ",
            };

            var list1 = q.Filter(args1).ToList();

            Assert.Single(list1);
            Assert.Equal("Fox", list1[0].StudentName);
        }

        {
            LikeArgs args1 = new LikeArgs
            {
                StudentName = "  F*   ",
            };

            var list1 = q.Filter(args1).ToList();

            Assert.Single(list1);
            Assert.Equal("Fox", list1[0].StudentName);
        }

    }

    [Fact]
    public void TestLike_NavigationProperty()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", Clazz = new Clazz{ ClazzName = "canine" }, No = 1 },
                new Student{ StudentName = "Dog", Clazz = new Clazz{ ClazzName = "canine" }, No = 2 },
                new Student{ StudentName = "Cat", Clazz = new Clazz{ ClazzName = "feline" }, No = 3 },
            };
        var q = list.AsQueryable();
        LikeArgs args1 = new LikeArgs
        {
            ClazzName = "can*   ",
        };

        var list1 = q.Filter(args1).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
    }

    [Fact]
    public void TestNotLike()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();
        NotLikeArgs args1 = new NotLikeArgs
        {
            StudentName = "*o*   ",
        };

        var list1 = q.Filter(args1).ToList();

        Assert.Single(list1);
        Assert.Equal("Cat", list1[0].StudentName);
    }

    [Fact]
    public void TestNotLike_NavigationProperty()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", Clazz = new Clazz{ ClazzName = "canine" }, No = 1 },
                new Student{ StudentName = "Dog", Clazz = new Clazz{ ClazzName = "canine" }, No = 2 },
                new Student{ StudentName = "Cat", Clazz = new Clazz{ ClazzName = "feline" }, No = 3 },
            };
        var q = list.AsQueryable();
        NotLikeArgs args1 = new NotLikeArgs
        {
            ClazzName = "can*   ",
        };

        var list1 = q.Filter(args1).ToList();

        Assert.Single(list1);
        Assert.Equal("Cat", list1[0].StudentName);
    }

    [Fact]
    public void TestGreaterThan()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        GreaterThanArgs args = new GreaterThanArgs
        {
            No = 1
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Dog", list1[0].StudentName);
        Assert.Equal("Cat", list1[1].StudentName);
    }

    [Fact]
    public void TestGreaterThanOrEqual()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        GreaterThanOrEqualArgs args = new GreaterThanOrEqualArgs
        {
            No = 2
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Dog", list1[0].StudentName);
        Assert.Equal("Cat", list1[1].StudentName);
    }

    [Fact]
    public void TestLessThan()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        LessThanArgs args = new LessThanArgs
        {
            No = 3
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
    }

    [Fact]
    public void TestLessThanOrEqual()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        LessThanOrEqualArgs args = new LessThanOrEqualArgs
        {
            No = 2
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
    }

    [Fact]
    public void TestIn()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        InArgs args = new InArgs
        {
            StudentNames = new[] { "Fox", "Dog", "Stranger" }
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
    }

    [Fact]
    public void TestIn_忽略空白字符串和NULL()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        InArgs args = new InArgs
        {
            StudentNames = new[] { " ", null }
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(3, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
        Assert.Equal("Cat", list1[2].StudentName);
    }

    [Fact]
    public void TestNotIn()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        NotInArgs args = new NotInArgs
        {
            StudentNames = new[] { "Cat", "Stranger" }
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
    }

    [Fact]
    public void TestExpression()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        ExpressionArgs args = new ExpressionArgs
        {
            StudentName = "o"
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
    }

    [Fact]
    public void TestExpression2()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1, Clazz = new Clazz { ClazzName = "visible" } },
                new Student{ StudentName = "Dog", No = 2, Clazz = new Clazz { ClazzName = "visible" } },
                new Student{ StudentName = "Cat", No = 3, Clazz = new Clazz { ClazzName = "invisible" } },
            };
        var q = list.AsQueryable();

        Expression2Args args = new Expression2Args
        {
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(2, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
    }

    [Fact]
    public void TestExpression3()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1, Clazz = new Clazz { ClazzName = "visible" } },
                new Student{ StudentName = "Dog", No = 2, Clazz = new Clazz { ClazzName = "visible" } },
                new Student{ StudentName = "Cat", No = 3, Clazz = new Clazz { ClazzName = "invisible" } },
            };
        var q = list.AsQueryable();

        Expression2Args args = new Expression2Args
        {
            ShowInvisibleClazz = true,
        };

        var list1 = q.Filter(args).ToList();

        Assert.Equal(3, list1.Count);
        Assert.Equal("Fox", list1[0].StudentName);
        Assert.Equal("Dog", list1[1].StudentName);
        Assert.Equal("Cat", list1[2].StudentName);
    }


    [Fact]
    public void FilterMethodOnSearchArgsShouldBeInvoked()
    {
        var list = new List<Student>
            {
                new Student{ StudentName = "Fox", No = 1 },
                new Student{ StudentName = "Dog", No = 2 },
                new Student{ StudentName = "Cat", No = 3 },
            };
        var q = list.AsQueryable();

        FilterArgs args = new FilterArgs();

        var list1 = q.Filter(args).ToList();

        Assert.Single(list1);
        Assert.Equal("Dog", list1[0].StudentName);

    }
}

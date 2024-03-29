﻿// Copyright 2022 王建军
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NHibernate.Linq;
using System.Collections;
using System.Collections.Specialized;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NHibernateUtils;


/// <summary>
/// 为使用 <see cref="SearchArgAttribute"/> 标记的类提供动态查询方法。
/// </summary>
public static class SimpleSearchExtensions
{
    /// <summary>
    /// 按照约定使用列表参数对象在查询上筛选。
    /// </summary>
    /// <typeparam name="T">数据源中的数据类型。</typeparam>
    /// <param name="q">查询对象，不必是 NHibernate 查询对象。</param>
    /// <param name="searchArgs">查询参数对象。</param>
    /// <returns></returns>
    public static IQueryable<T> Filter<T>(this IQueryable<T> q, object searchArgs)
    {
        if (searchArgs is null)
        {
            throw new ArgumentNullException(nameof(searchArgs));
        }

        Type argsType = searchArgs.GetType();

        // 如果 searchArgs 上有 Filter 方法，则首先调用 Filter 方法
        MethodInfo? filterMethodInfo = argsType.GetMethod("Filter", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (filterMethodInfo != null
            && filterMethodInfo.GetParameters().Length == 1 && filterMethodInfo.GetParameters()[0].ParameterType == typeof(IQueryable<T>)
            && filterMethodInfo.ReturnType == typeof(IQueryable<T>))
        {
            q = (IQueryable<T>)filterMethodInfo.Invoke(searchArgs, new[] { q })!;
            if (q is null)
            {
                throw new InvalidOperationException("Filter 方法不能返回 null。");
            }
        }

        var props = argsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var argProp in props)
        {
            SearchArgAttribute? attr = argProp.GetCustomAttribute<SearchArgAttribute>();
            if (attr is null)
            {
                continue;
            }

            object? arg = GetSearchArg(argProp, searchArgs);

            //  查询参数属性为 null 表示忽略这个条件
            if (arg is null && attr.SeachMode != SearchMode.Expression)
            {
                continue;
            }

            // 源属性
            string sourcePropertyName = attr.SourceProperty ?? argProp.Name;

            switch (attr.SeachMode)
            {
                case SearchMode.Auto:
                    if (argProp.PropertyType == typeof(string))
                    {
                        q = q.Where(CreateLikeExpression<T>(sourcePropertyName, (string?)arg, false));
                    }
                    else
                    {
                        q = q.Where($"{sourcePropertyName} == @0", arg);
                    }
                    break;
                case SearchMode.Equal:
                    q = q.Where($"{sourcePropertyName} == @0", arg);
                    break;
                case SearchMode.NotEqual:
                    q = q.Where($"{sourcePropertyName} != @0", arg);
                    break;
                case SearchMode.Like:
                    q = q.Where(CreateLikeExpression<T>(sourcePropertyName, (string?)arg, false));
                    break;
                case SearchMode.NotLike:
                    q = q.Where(CreateLikeExpression<T>(sourcePropertyName, (string?)arg, true));
                    break;
                case SearchMode.GreaterThan:
                    q = q.Where($"{sourcePropertyName} > @0", arg);
                    break;
                case SearchMode.GreaterThanOrEqual:
                    q = q.Where($"{sourcePropertyName} >= @0", arg);
                    break;
                case SearchMode.LessThan:
                    q = q.Where($"{sourcePropertyName} < @0", arg);
                    break;
                case SearchMode.LessThanOrEqual:
                    q = q.Where($"{sourcePropertyName} <= @0", arg);
                    break;
                case SearchMode.In:
                    {
                        if (arg is IEnumerable values && values.ToDynamicArray().Length > 0)
                        {
                            q = q.Where($"@0.Contains({sourcePropertyName})", values);
                        }
                    }
                    break;
                case SearchMode.NotIn:
                    {
                        if (arg is Array arr && arr.Length > 0)
                        {
                            q = q.Where($"@0.Contains({sourcePropertyName}) == false", arr);
                        }
                    }
                    break;
                case SearchMode.Expression:
                    string exprPropName = argProp.Name + "Expr";

                    PropertyInfo? exprProp = argsType.GetProperty(exprPropName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (exprProp is null)
                    {
                        throw new InvalidOperationException($"参数类型 {typeof(T)} 上为 {sourcePropertyName} 提供表达式的 {exprPropName} 属性不存在");
                    }

                    object? e = exprProp.GetValue(searchArgs, null);
                    if (e is null)
                    {
                        continue;
                    }

                    if (e is Expression<Func<T, bool>> expr)
                    {
                        q = q.Where(expr);
                    }
                    else
                    {
                        throw new InvalidOperationException($"参数类型 {typeof(T)} 的 {exprPropName} 属性的类型不是 {typeof(Expression<Func<T, bool>>)}。");
                    }
                    break;
                default:
                    throw new NotSupportedException("不支持的查询方式。");
            }
        }

        return q;
    }


    /// <summary>
    /// 对查询排序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="q">要排序的查询对象</param>
    /// <param name="sortInfo">排序信息。键为要排序的属性，值为排序顺序，
    /// 值为 desc、descend、descending 表示降序，否则为升序。值不区分大小写。
    /// 例如 { "A" : "desc", "B": null } 表示先按属性 A 降序排序，再按属性 B 升序排序。</param>
    /// <returns></returns>
    internal static IQueryable<T> OrderBy<T>(this IQueryable<T> q, OrderedDictionary? sortInfo)
    {
        string? orderBy = GetOrderByClause(sortInfo);
        if (string.IsNullOrWhiteSpace(orderBy) == false)
        {
            q = q.OrderBy(orderBy);
        }
        return q;
    }


    /// <summary>
    /// 对查询分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nhQuery">NHibernate 查询对象</param>
    /// <param name="currentPage">基于 1 的页号码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns></returns>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> nhQuery, int currentPage, int pageSize)
    {
        if (currentPage < 1)
        {
            currentPage = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        var total = await nhQuery.CountAsync().ConfigureAwait(false);
        if (total == 0)
        {
            return new PagedList<T>(new List<T>(), 1, pageSize, 0);
        }

        int start = (currentPage - 1) * pageSize;
        var list = await nhQuery.Skip(start)
            .Take(pageSize)
            .ToListAsync()
            .ConfigureAwait(false);
        return new PagedList<T>(list, currentPage, pageSize, total);
    }

    /// <summary>
    /// 在查询上进行筛选、排序、分页。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nhQuery">NHibernate 查询对象</param>
    /// <param name="searchArgs">搜索参数</param>
    /// <param name="sort">排序信息</param>
    /// <param name="currentPage">基于 1 的页号码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns></returns>
    public static Task<PagedList<T>> SearchAsync<T>(this IQueryable<T> nhQuery, object searchArgs, OrderedDictionary? sort, int? currentPage, int? pageSize)
    {
        return nhQuery.Filter(searchArgs).OrderBy(sort).ToPagedListAsync(currentPage ?? 1, pageSize ?? 10);
    }

    /// <summary>
    /// 在查询上进行筛选、排序、分页。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nhQuery">NHibernate 查询对象</param>
    /// <param name="searchArgs">搜索参数</param>
    /// <param name="sort">排序信息</param>
    /// <param name="currentPage">基于 1 的页号码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns></returns>
    public static Task<PagedList<T>> SearchAsync<T>(this IQueryable<T> nhQuery, object searchArgs, string? sort, int? currentPage, int? pageSize)
    {
        nhQuery = nhQuery.Filter(searchArgs);
        if (!string.IsNullOrWhiteSpace(sort))
        {
            nhQuery = nhQuery.OrderBy(sort);
        }
        return nhQuery.ToPagedListAsync(currentPage ?? 1, pageSize ?? 10);
    }

    internal static object? GetSearchArg(PropertyInfo prop, object searchArgs)
    {
        object? val = prop.GetValue(searchArgs);

        // 处理字符串类型的查询条件
        if (prop.PropertyType == typeof(string))
        {
            string? str = (string?)val;

            // 忽略空白字符串
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            str = str.Trim();

            // like 需专门处理
            var attr = prop.GetCustomAttribute<SearchArgAttribute>();
            if (attr?.SeachMode == SearchMode.Like || attr?.SeachMode == SearchMode.Auto)
            {
                if (!str.Contains("*") && !str.Contains("?"))
                {
                    str = $"*{str}*";
                }
                str = str.Replace('*', '%').Replace('?', '_');
            }

            val = str;
        }
        else if (val is IEnumerable<string> strings)
        {
            val = strings.Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }

        return val;
    }

    static string? GetOrderByClause(OrderedDictionary? sort)
    {
        if (sort is null || sort.Count == 0)
        {
            return null;
        }
        StringBuilder sb = new StringBuilder();
        foreach (DictionaryEntry entry in sort)
        {
            string sortOrder = entry.Value?.ToString()?.ToLower() switch
            {
                "desc"
                or "descend"
                or "descending" => "DESC",
                _ => "ASC"
            };

            sb.Append($"{entry.Key} {sortOrder}, ");
        }
        sb.Remove(sb.Length - 2, 2);
        return sb.ToString();
    }

    static Expression<Func<T, bool>> CreateLikeExpression<T>(string sourcePropertyPath, string? likePattern, bool notLike)
    {
        // 要得到的表达式：x => SqlMethods.Like(x.Foo.StringField, "a%")
        MethodInfo? mi = typeof(SqlMethods).GetMethod("Like", new Type[] { typeof(string), typeof(string) });
        if (mi is null)
        {
            throw new InvalidOperationException();
        }
        ParameterExpression x = Expression.Parameter(typeof(T), "x");  // x

        // 变量 propertyNames 的值: 
        // [0]: Foo
        // [1]: StringField
        var propertyNames = sourcePropertyPath.Split(".").Select(x => x.Trim());
        Expression parameter = x;
        foreach (var propName in propertyNames)
        {
            // [0]: x.Foo
            // [1]: x.Foo.StringField
            parameter = Expression.Property(parameter, propName);
        }
        Expression stringField = parameter;

        Expression pattern = Expression.Constant(likePattern, typeof(string));  // 'a%'
        Expression like = Expression.Call(null, mi, stringField, pattern); //  SqlMethods.Like(x.Foo.StringField, "a%")
        if (notLike)
        {
            like = Expression.Not(like);
        }
        return Expression.Lambda<Func<T, bool>>(like, new ParameterExpression[] { x });
    }

}

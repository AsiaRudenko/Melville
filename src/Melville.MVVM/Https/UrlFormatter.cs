﻿using  System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Melville.MVVM.Functional;

namespace Melville.MVVM.Https
{
  public static class UrlFormatter
  {
    public static string ConcatUrlParts(params string[] strings) =>
      ConcatUrlParts((IEnumerable<string>) strings);

    public static string ConcatUrlParts(IEnumerable<string> strings) =>
      string.Join("/",strings.Select(i => i.Trim('\\', '/')));

    public static string AssembleUrl(string prefix, object? parameters = null) => 
      prefix + PrefixedUrlList(ToUrlList(parameters));

    private static string PrefixedUrlList(string urlList) => 
      (string.IsNullOrWhiteSpace(urlList)?"":"?") + urlList;

    public static string ToUrlList(object? parameters) =>
      parameters == null
        ? ""
        : parameters.GetType().GetProperties()
          .Select(i => ParameterValue(parameters, i))
          .Interleave("&").ConcatenateStrings();

    public static string ParameterValue(object parameters, PropertyInfo property)
    {
      var propName = Uri.EscapeDataString(property.Name);
      var valAsEnumerable = ToParamArray(property.GetValue(parameters));
      return String.Join("&", valAsEnumerable.OfType<object>()
        .Select(i => $"{propName}={Uri.EscapeDataString(i.ToString()??"")}"));
    }

    private static IEnumerable ToParamArray(object? value)
    {
      var valAsEnumerable = value switch
      {
        null => Array.Empty<object>(),
        string s => new object[] {s},
        IEnumerable enumerable => enumerable,
        var obj => new[] {obj}
      };
      return valAsEnumerable;
    }

    public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePair(object parameters) =>
      (parameters ?? new object())
      .GetType()
      .GetProperties()
      .Select(i => new KeyValuePair<string, string>(i.Name, i.GetValue(parameters)?.ToString()??""));

    public static string MakeFolderList(params object[] folders) =>
      folders
        .Select(i=>Uri.EscapeDataString(i.ToString()??""))
        .Interleave("/")
        .Concat("/")
        .ConcatenateStrings();

    public static string MakePeerUri(this Uri source, string suffix) => 
      $"{source.Scheme}://{source.Authority}{suffix}";
  }
}
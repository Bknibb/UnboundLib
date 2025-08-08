using System;
using System.Reflection;

[assembly: Github("Bknibb", "UnboundLib")]

[AttributeUsage(AttributeTargets.Assembly)]
public class GithubAttribute : Attribute
{
    public string Owner { get; }
    public string Repo { get; }

    public GithubAttribute(string owner, string repo)
    {
        Owner = owner;
        Repo = repo;
    }
}

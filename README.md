

TickSpec.Build generates test classes to execute [TickSpec](https://github.com/fsprojects/TickSpec) (Gherkin/BDD)
based specifications

```bash
TickSpec.Build fixtures FeatureFixtures.fs
```

It automatically integrates into the build process as "BeforeCompile" target when integrated via NuGet package.


## HTML documentation

TickSpec.Build additionally supports generating HTML documents for the feature files

```bash
TickSpec.Build doc ./src ./html
```

When generating the HTML files to the output location the F# project local folders are preserved.

Using ``--toc html`` a HTML table of contents and with ``--toc json`` a Json table of contents can be generated.

To integrate the HTML generation into the build process set the property ``FeatureFileHtmlOutput`` to the location 
the HTML files should be generated too. By default, only the feature files local to this project are considered.
You can change this by setting the property ``FeatureFileHtmlInput``.


## Story behind this project

The following articles tell the story behind this project:

- [Lean BDD and Code Generation](http://www.plainionist.net/TickSpec-with-Code-Generation/)
- [Lean BDD with even more Code Generation](http://www.plainionist.net/TickSpec-More-CodeGen/)

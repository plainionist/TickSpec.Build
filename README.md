

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

### Styling

The generated HTML documents intentionally only contain HTML fragments of type "article" so that those 
articles can easily be integrated in an existing HTML documentation. 

These articles provide the following CSS classes for styling:

- **gherkin-keyword** applies to the keywords like GIVEN, WHEN, THEN
- **gherkin-scenario-body** applies to the body of a scenario
- **gherkin-scenario** applies to a complete scenario
- **gherkin-tags** applies to the tags attached to scenarios
- **gherkin-description** applies to a comment provided above a scenario
- **gherkin-scenario-title** applies to the title of a scenario
- **gherkin-feature-title** applies to the feature title

If you want to use the generated articles as a standalone documentation use ``--toc html`` to generate a 
standalone HTML document. Put a ``style.css`` next to the ``ToC.html`` to define the CSS classes listed above

### MsBuild integration

To integrate the HTML generation into your MsBuild based build process set the property ``FeatureFileHtmlOutput``
to the location the HTML files should be generated too. By default, only the feature files local to this project
are considered. You can change this by setting the property ``FeatureFileHtmlInput``.

The format of the table of contents can be set using property ``TickSpecBuildTocFormat``.


## Story behind this project

The following articles tell the story behind this project:

- [Lean BDD and Code Generation](http://www.plainionist.net/TickSpec-with-Code-Generation/)
- [Lean BDD with even more Code Generation](http://www.plainionist.net/TickSpec-More-CodeGen/)

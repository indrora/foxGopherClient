ForkedFoxGopherClient: original readme.md below \/
===============

FoxGopherClient
===============

A simple client for the GOPHER:// protocol.

I started this project long ago when I was bored and wanted to learn much of WPF that I hadn't already. It is, for the most part, feature-complete.

Things it does
--------------

* Handles 90% of the standard GOPHER line types with graceful fallback to those it does not understand.
* Understands image types, as well as knows how to align things
* Fails gracefully.

Things it doesn't do
--------------------

* Make coffee
* Download files asynchronously (the hooks are there, I've just played with too many ideas)
* Handle inline file handling.
* Bookmarking and other settings (Hooks are all there, not quite ready for prime-time though)

Some credits
------------

I've used (see: nearly blatantly stolen) several image and XAML resources in the project; these are just what I can recall at 0-dark-thirty at night.

* Main app icon: Firefox icon by Mattahan (GANT icon set)
* Either Silk icon sets from Fan Fam Fam
* WhistlerBlue theme from Microsoft (Publicly available, but you don't ever know with them!)
* Nav Buttons are ripped from the Navigation Chrome in the toolset.

Compiling this beast
--------------------

The code is all in vanilla .NET 3.5 (C# 3) and should compile on anything that can compile .NET 3.5 WPF code. VS08 should open the project kindly.

Remarks
-------

This is *really* bad code for the most part. It pulls mean tricks with the WPF visual helpers at times (see the function <code>IndexSubmit</code> in Window1.xaml.cs ) and occasionally doesn't do any form of documentation. It makes lots of assumptions about the format of files and generally doesn't make many allowances for the horrible things some servers do.

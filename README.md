# Peakboard Extension Samples
This repo contains samples for the Peakboard Extension Kit. The following list contains a quick explanation for each sample. The complexity of the samples rises from easy to difficult.
Here is a step-by-step guide how to run the samples:

1. Compile the solution (maybe update / restore the Peakboard Extensions Nuget package)
2. Put the binary output PLUS the extensions.xml file PLUS all referenced assemblies in a single zip file
3. Start you Peakboard designer and use the "Manage Extensions" button to add the extension to your local data source collection and enjoy it.

# Cat Facts
This sample is a supereasy sample and ideal for beginners to explore the basic concepts. It shows how to provide a simple, fixed list in Peakboard but already gives the user the option to adjust a list property. 

# SQL Server
This is a sample to show, how a typical database extension works. It's fully functional, however please don#t use it in real life.... it's for programmers only.

# Airport Weather Conditions
This sample shows, how to create your own dialog and your own UI. The user can download weather information from various airports in Germany.

# Hue
This is a cool real world example to access a Phillips Hue Bridge and control light bulbs. You can get a list of available lights and switch them on and off. And also define the brightness of a light bulb. This is a cool exmaple on how to use custom functions within a custom list.

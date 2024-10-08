# Peakboard extension for POS printers supporting ESC/POS or ZPL protocol
This extension provides two custom lists, one supporting the ESC/POS protocol from EPSON, the other supporting the ZPL from Zebra. POS printers that support either of these protocols can be integrated into a Peakboard application without the need to install a driver.

The ESC/POS source also allows to add the POS printer via the USB port using a virtual serial port. To use the the printer directly a printer driver must be installed. This data source also provides an easy to use and simple way to generate the print format (see below).

Both data sources offer a function called print. The function takes the formatted text as the first parameter and in the case of ESC/POS you can pass a second parameter with tokens to replace in the text.

You can use the Zebra ZPL protocol to create more sophisticated labels by using the direct protocol formats.

# ESC/POS format

The ESC/POS custom format provides a set of easy-to-use commands that are always enclosed in **\~(COMMAND)\~**. The following commands are currently supported:

**\~(CentralAlign)\~**\
Centers all text following the command.

**\~(LeftAlign)\~**\
Left-aligns all text following the command.

**\~(RightAlign)\~**\
Right-aligns all text following the command.

**\~(Style:Bold,Italic,DoubleWidth,DoubleHeight)\~**\
Sets the style of the following text. Not all printers understand all styles or combinations of styles.

**\~(Barcode:CODE128,12345)\~**\
Prints a barcode of type CODE128 with the numbers 12345. You can also use ITF as the barcode type.

**\~(FullCut)\~**\
Full cut after this command.

**\~(FeedLines:2)\~**\
Feed 3 lines.

**\~(FullCutAfterFeed:3)\~**\
Full cut after a feed of 3 lines.

## Samples

	~(CentralAlign)~
	Heading with #[param1]#
	~(Style:Bold)~
	Row 2
	Row 3
	~(LeftAlign)~
	~(Style:DoubleHeight)~
	~(Barcode:CODE128,1234567890)~
	Row 4
	~(Style:Bold,Italic,DoubleHeight)~
	Row 5
	~(FullCutAfterFeed:1)~
 
## Virtual printer apps for testing
1. iOS App 'Virtual Thermal Printer' by Pascal Kimmel used as a virtual printer for the ESC/POS protocol. Not all commands are supported.
2. Virtual ZPL Printer by Daniel Porrey used as a virtual printer for the Zebra ZPL protocol. Available on GitHub. 

## Screenshots
![image](https://github.com/user-attachments/assets/56dff34a-c3bd-45fe-bada-4459069fe3df)
![image](https://github.com/user-attachments/assets/15b8d1d0-af07-4838-bc85-4654c5db3981)
![image](https://github.com/user-attachments/assets/f9391d90-4714-40ef-8211-bedf3cad6349)
![image](https://github.com/user-attachments/assets/dafd8455-dfc6-4e3e-a455-0135d9fc76ae)
![image](https://github.com/user-attachments/assets/2fda0f64-1b96-4118-a5bb-5a36395cef40)





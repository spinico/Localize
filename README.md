
![Localize WPF extension][logo] **Localize**
=======

WPF extension for resources localization. 

#### **Sample application**
The sample application shows how to use most of the features.

![Sample application preview](https://github.com/spinico/Localize/blob/master/Images/demo.gif?raw=true)

#### **Example use for string resource**
To localize a string resource in a WPF application:

 1. Create a default resource file (ex. `resources.resx`) in your solution.
 2. Add a string resource to it (ex. MyString = "Hello World!")
 3. Add another resource file for a specific culture (ex. fr) using the same name with the culture as a suffix (ex. `resources.fr.resx`)
 4. Add the localized string to this second resource file (ex. MyString = "Bonjour le monde!")
 5. In a XAML file, add the `Localize.DefaultResource` attribute to the root element (usually on a Window or UserControl) and set it to the name (including namespace) of your default resource file without the extension. For example, an application having the namespace "MyApplication" would look like:	
	```
	<Window ...
	Localize.DefaultResource="MyApplication.resources"
	/>
	```
	
 6. Now declare a `TextBlock` control in your XAML and set its `Text` property using the `Localize` extension: 
	```
	<TextBlock Text="{Localize MyString}"/>
	```
	This is equivalent to:
	```
	<TextBlock Text="{Localize MyString, Resource=MyApplication.resources}"/>
	```
	Since a default resource was previously set on the root element, there is no need to specify a *Resource* attribute explicitly on a child element (unless the resources file differs).
 7. To switch culture at runtime, set the `CultureManager.CurrentCulture` property to one of your defined resource culture, for example:

	```
	CultureManager.CurrentCulture = new CultureInfo("fr");
	```

----------
The MIT License (MIT)


[logo]: https://github.com/spinico/Localize/blob/master/Images/logo.png?raw=true "Localize WPF extension"
![Localize WPF extension][logo] ***Localize***
=======

A WPF extension for resources localization. 

#### **Basic usage for string resources**
To localize a string resource in a WPF application:

 1. Create a default resource file (ex. `resources.resx`) in your solution.
 2. Add a string resource to it (ex. MyString = "Hello World!")
 3. Add another resource file for a new culture (ex. fr) using the same name but using the specified culture as a suffix (ex. `resources.fr.resx`)
 4. Add the localized version to this second resource file (ex. MyString = "Bonjour le monde!")
 5. Now declare a `TextBlock` control in your XAML and set its `Text` property using the `Localize` extension: 
	```
	<TextBlock Text="{Localize MyString}"/>
	```

 6. To switch culture at runtime, set the `CultureManager.CurrentCulture` property to one of your defined resource culture, for example:
 ```
CultureManager.CurrentCulture = new CultureInfo("fr");
 ```


#### **Sample application**
The sample application shows most of the features available.

![Sample application preview](https://github.com/spinico/Localize/blob/master/Images/demo.gif?raw=true)

----------
The MIT License (MIT)


[logo]: https://github.com/spinico/Localize/blob/master/Images/logo.png?raw=true "Localize WPF extension"
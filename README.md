#Molten Mercury

*Note: The original project page on Codeplex along with more comprehensive documentation is [this way](http://moltenmercury.codeplex.com).*

**Project Molten Mercury** is a set of software for creating Japanese-anime-styled characters from various character parts. The project was inspired by a software named キャラクターなんとか機, which is also a character creator written by its Japanese author [緋龍華 麒麟](http://khmix.sakura.ne.jp/).

![](https://raw2.github.com/jluchiji/Molten-Mercury/master/Images/mainwindow.PNG)

Molten Mercury has numerous improvements over キャラクターなんとか機, which are:

 - **Support for non-Japanese OS**
 Since MoltenMercury was developed in C# and .Net, all text will be displayed correctly in OS of any language that supports .Net. 

 - **Makes localization easy**
 Molten Mercury supports external locale file (locale.xml). Translate it, and you have translated the MoltenChara program. Please note that MCU does not support translation since it is a command line tool for advanced users.

 - **Provides more comprehensive color management**
 Along with numerous presets, Molten Mercury allows user to change colors by adjusting Hue, Saturation and Lightness. Color changes are not limited to skin, clothes, eyes and hair anymore: by properly setting up Color Groups it is possible to assign independent colors to ribbons, wristbands and many more elements of the character.

 - **Has a more flexible character part management**
Images representing character parts are no longer maintained solely based on their directories: MoltenMercury uses a data file for recording metadata of each character part: images used by that part, its layer index and color group. Meanwhile, character parts are no longer grouped into predefined categories: there can be as many categories as you want!

 - **Can save characters into self-contained archives**
You can choose to save a character along with all the images into a single file. Even if you send this character to a friend that doesn't have the same character resources you have, no problem will arise since everything needed is in that file!

 - **Allows creation of patches that can be deployed by others**
No more directories and copying them around! Artists can create their artwork, organize them into character parts, assign layer parameters, and set up their color groups. All the files and metadata is saved into a MoltenChara Package which can be directly browsed by MoltenChara. These patches can be merged into any existing character resource set in just a few clicks, and when doing so you don't have to worry about scripting and metadata. Don't like a patch that you installed? Every patch is deployed into its own folder in ***patches*** directory. Simply delete it and verify your character resources, and that patch will be gone! Meanwhile, artists can update their patches easily since installing patches with the same character parts will overwrite existing resources. Even characters saved as self-contained packages can be added to a resource set just like a patch!


 - **Options to lock a saved character**
 Want to show your best work to somebody, but don't want it to be modified? MoltenChara allows you to lock down a saved character. MoltenChara does not allow anybody to modify a locked character: it's readonly.

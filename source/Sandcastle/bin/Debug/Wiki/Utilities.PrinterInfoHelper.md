            
This helper fills out the info text for the WBIPrinterRequirements part module. During the game startup, it asks part modules to GetInfo. WBIPrinterRequirements is no exception. However, because it relies on the PartLoader to obtain information about prerequisite components, WBIPrinterRequirements can't completely fill out its info. We get around the problem by waiting until we load into the editor, and manually changing the ModuleInfo associated with WBIPrinterRequirements. It's crude but effective.
        


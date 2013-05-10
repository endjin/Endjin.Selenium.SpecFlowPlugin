<?xml version="1.0" encoding="utf-8"?>
<configurationSectionModel xmlns:dm0="http://schemas.microsoft.com/VisualStudio/2008/DslTools/Core" dslVersion="1.0.0.0" Id="8c0a7e95-4704-4b05-a971-c3c11c868694" namespace="Endjin.Selenium.SpecFlowPlugin.Configuration" xmlSchemaNamespace="urn:Endjin.Selenium.SpecFlowPlugin.Configuration" xmlns="http://schemas.microsoft.com/dsltools/ConfigurationSectionDesigner">
  <typeDefinitions>
    <externalType name="String" namespace="System" />
    <externalType name="Boolean" namespace="System" />
    <externalType name="Int32" namespace="System" />
    <externalType name="Int64" namespace="System" />
    <externalType name="Single" namespace="System" />
    <externalType name="Double" namespace="System" />
    <externalType name="DateTime" namespace="System" />
    <externalType name="TimeSpan" namespace="System" />
  </typeDefinitions>
  <configurationElements>
    <configurationElement name="CredentialsElement" namespace="Endjin.Selenium.SpecFlowPlugin.Configuration">
      <attributeProperties>
        <attributeProperty name="AccessKey" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="accessKey" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="UserName" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="userName" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Url" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="url" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationSection name="SauceLabsSection" namespace="Endjin.Selenium.SpecFlowPlugin.Configuration" codeGenOptions="Singleton, XmlnsProperty" xmlSectionName="sauceLabsSection">
      <elementProperties>
        <elementProperty name="Credentials" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="credentials" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/CredentialsElement" />
          </type>
        </elementProperty>
        <elementProperty name="Capabilities" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="capabilities" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/Capability" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationSection>
    <configurationElementCollection name="Capability" namespace="Endjin.Selenium.SpecFlowPlugin.Configuration" xmlItemName="capability" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <itemType>
        <configurationElementMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/CapabilityElement" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="CapabilityElement" namespace="Endjin.Selenium.SpecFlowPlugin.Configuration">
      <attributeProperties>
        <attributeProperty name="Key" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="key" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Browser" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="browser" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="BrowserVersion" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="browserVersion" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Platform" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="platform" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/8c0a7e95-4704-4b05-a971-c3c11c868694/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
  </configurationElements>
  <propertyValidators>
    <validators />
  </propertyValidators>
</configurationSectionModel>
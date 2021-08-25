export interface USBDeviceFound {
    usbVersionMajor: number,
    usbVersionMinor: number,
    usbVersionSubminor: number,
    deviceClass: number,
    deviceSubclass: number,
    deviceProtocol: number,
    vendorId: number,
    productId: number,
    deviceVersionMajor: number,
    deviceVersionMinor: number,
    deviceVersionSubminor: number,
    manufacturerName: string,
    productName: string,
    serialNumber: string,
    configuration: USBConfiguration | null,
    configurations: USBConfiguration[],
    opened: boolean
}
export interface USBConfiguration { configurationValue: number, configurationName: string, interfaces: USBInterface[] }
export interface USBInterface { interfaceNumber: number, alternate: USBAlternateInterface | null, alternates: USBAlternateInterface[], claimed: boolean }
export interface USBAlternateInterface { alternateSetting: number, interfaceClass: number, interfaceSubclass: number, interfaceProtocol: number, interfaceName: string, endpoints: USBEndpoint[] }
export interface USBDeviceFilter { vendorId: number, productId: number, classCode: any, subClassCode: any, protocolCode: any, serialNumber: string }
export interface USBRequestDeviceOptions { filters: USBDeviceFilter[] }
export interface USBEndpoint { endpointNumber: number, direction: USBDirection, type: USBEndpointType, packetSize: number }
export interface USBTransferResult { status: USBTransferStatus }
export interface USBInTransferResult extends USBTransferResult { data: any }
export interface USBOutTransferResult extends USBTransferResult { bytesWritten: number }
export interface USBControlTransferParameters { requestType: USBRequestType, recipient: USBRecipient, request: number, value: number, index: number }

export enum USBDirection { "in", "out" }
export enum USBEndpointType { "bulk", "interrupt", "isochronous" }
export enum USBTransferStatus { "ok", "stall", "babble" }
export enum USBRequestType { "standard", "class", "vendor" }
export enum USBRecipient { "device", "interface", "endpoint", "other" }

export function ParseUSBDevice(rawDevice: any): USBDeviceFound {
    return {
        usbVersionMajor: rawDevice.usbVersionMajor,
        usbVersionMinor: rawDevice.usbVersionMinor,
        usbVersionSubminor: rawDevice.usbVersionSubminor,
        deviceClass: rawDevice.deviceClass,
        deviceSubclass: rawDevice.deviceSubclass,
        deviceProtocol: rawDevice.deviceProtocol,
        vendorId: rawDevice.vendorId,
        productId: rawDevice.productId,
        deviceVersionMajor: rawDevice.deviceVersionMajor,
        deviceVersionMinor: rawDevice.deviceVersionMinor,
        deviceVersionSubminor: rawDevice.deviceVersionSubminor,
        manufacturerName: rawDevice.manufacturerName,
        productName: rawDevice.productName,
        serialNumber: rawDevice.serialNumber,
        configuration: rawDevice.configuration != null ? ParseUSBConfiguration(rawDevice.configuration) : null,
        configurations: rawDevice.configurations.map(raw => ParseUSBConfiguration(raw)),
        opened: rawDevice.opened
    };
}

function ParseUSBConfiguration(rawConfiguration: any): USBConfiguration {
    return {
        configurationValue: rawConfiguration.configurationValue,
        configurationName: rawConfiguration.configurationName,
        interfaces: rawConfiguration.interfaces.map(raw => ParseUSBInterface(raw))
    };
}

function ParseUSBInterface(rawInterface: any): USBInterface {
    return {
        interfaceNumber: rawInterface.interfaceNumber,
        alternate: rawInterface.alternate ? ParseUSBAlternateInterface(rawInterface.alternate) : null,
        alternates: rawInterface.alternates.map(raw => ParseUSBAlternateInterface(raw)),
        claimed: rawInterface.claimed
    };
}

function ParseUSBAlternateInterface(rawAlternate: any): USBAlternateInterface {
    return {
        alternateSetting: rawAlternate.alternateSetting,
        interfaceClass: rawAlternate.interfaceClass,
        interfaceSubclass: rawAlternate.interfaceSubclass,
        interfaceProtocol: rawAlternate.interfaceProtocol,
        interfaceName: rawAlternate.interfaceName,
        endpoints: rawAlternate.endpoints.map(raw => ParseUSBEndpoint(raw))
    };
}

function ParseUSBEndpoint(rawEndpoint: any): USBEndpoint {
    return {
        endpointNumber: rawEndpoint.endpointNumber,
        direction: <USBDirection>rawEndpoint.direction,
        type: <USBEndpointType>rawEndpoint.type,
        packetSize: rawEndpoint.packetSize
    };
}
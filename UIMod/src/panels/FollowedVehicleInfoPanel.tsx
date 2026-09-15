import React, { useRef, useEffect, useState } from 'react';
import { bindValue, useValue } from "cs2/api";

const UISettingsGroupOptions$ = bindValue<string>('fpc', 'UISettingsGroupOptions');
const FollowedEntityInfo$ = bindValue<string>('fpc', 'FollowedEntityInfo');
const ShowCrosshair$ = bindValue<boolean>('fpc', 'ShowCrosshair');

interface TranslationProps {
    nameLabel: string | null;
    speedLabel: string | null;
    altitudeLabel: string | null;
    vehicleTypeLabel: string | null;
    resourcesLabel: string | null;
    actionLabel: string | null;
    passengersLabel: string | null;
}

interface FollowedVehicleInfoPanelProps {
    translation: TranslationProps;
}

const FollowedVehicleInfoPanel: React.FC<FollowedVehicleInfoPanelProps> = ({ translation }) => {
    const speedDivRef = useRef<HTMLDivElement>(null);
    const [speedWidth, setSpeedWidth] = useState<number | undefined>(undefined);

    const panelContentRef = useRef<HTMLDivElement>(null);

    const uiSettingsGroupOptions = useValue(UISettingsGroupOptions$);
    const followedEntityInfo = useValue(FollowedEntityInfo$);
    const showCrosshair = useValue(ShowCrosshair$);

    const parsedSettings = JSON.parse(uiSettingsGroupOptions);
    const showInfoPanel = parsedSettings.ShowInfoBox;
    const showSpeed = parsedSettings.ShowSpeed;
    const showVehicleType = parsedSettings.ShowVehicleType;
    const showExtraInfo = parsedSettings.ShowExtraInfo;
    const infoBoxSize = parsedSettings.InfoBoxSize;
    const setUnits = parsedSettings.SetUnits;

    const parsedSpeed = JSON.parse(followedEntityInfo).currentSpeed;
    const parsedAltitude = JSON.parse(followedEntityInfo).altitude;
    const parsedUnits = JSON.parse(followedEntityInfo).unitsSystem;
    const parsedPassengers = JSON.parse(followedEntityInfo).passengers;
    const parsedResources = JSON.parse(followedEntityInfo).resources;
    const vehicleType = JSON.parse(followedEntityInfo).vehicleType;
    const citizenName = JSON.parse(followedEntityInfo).citizenName;
    const citizenAction = JSON.parse(followedEntityInfo).citizenAction;

    let formattedSpeed: string;

    const toMph = (s: number) => Math.round(s * 1.26) + " mph";
    const toKmh = (s: number) => Math.round(s * 1.8) + " km/h";

    //if mod unit override is off
    if (setUnits === 0) {
        formattedSpeed = parsedUnits === 1 ? toMph(parsedSpeed) : toKmh(parsedSpeed);
    }
    else if (setUnits === 2) {
        formattedSpeed = toMph(parsedSpeed)
    }
    else {
        formattedSpeed = toKmh(parsedSpeed)
    }

    let formattedAltitude: string;

    const toFeet = (a: number) => Math.round(a * 3.28) + " ft";
    const toMeters = (a: number) => Math.round(a) + " m";

    if (setUnits === 0) {
        formattedAltitude = parsedUnits === 1 ? toFeet(parsedAltitude) : toMeters(parsedAltitude);
    }
    else if (setUnits === 2) {
        formattedAltitude = toFeet(parsedAltitude)
    }
    else {
        formattedAltitude = toMeters(parsedAltitude)
    }

    let formattedResources = Math.round(parsedResources*100) + "%";

    const hasSpeedRow = parsedSpeed !== -1 && showSpeed;

    useEffect(() => {
        if (!hasSpeedRow || !showInfoPanel || speedWidth !== undefined) {
            return;
        }

        let cancelled = false;
        const updateWidth = () => {
            if (cancelled) {
                return;
            }
            if (speedDivRef.current) {
                const width = speedDivRef.current.offsetWidth;
                console.log("speedDivRef:", width);
                if (width > 0) {
                    setSpeedWidth(width / (window.innerWidth / 1920) + 40 + 5);
                    return;
                }
            }
            requestAnimationFrame(updateWidth);
        };

        requestAnimationFrame(updateWidth);
        return () => { cancelled = true; };
    }, [hasSpeedRow, showInfoPanel]);

    if (!showInfoPanel) {
        return null;
    }

    const infoBoxSizeClass = infoBoxSize === 0 ? 'small' :
        infoBoxSize === 2 ? 'large' : '';

    // Create content without rendering it yet
    const renderPanelContent = () => (
        <div className="item_bZY" ref={panelContentRef}>
            {citizenName !== null && showExtraInfo && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.nameLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{citizenName}</div>
                </div>
            )}
            {hasSpeedRow && (
                <div
                    ref={speedDivRef}
                    className={`fpcc-info-group-speed-padding ${infoBoxSizeClass}`}
                    style={{ width: speedWidth ? `${speedWidth}rem` : 'auto' }}
                >
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.speedLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{formattedSpeed}</div>
                </div>
            )}
            {parsedAltitude !== -1 && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.altitudeLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{formattedAltitude}</div>
                </div>
            )}
            {vehicleType !== null && showVehicleType && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.vehicleTypeLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{vehicleType}</div>
                </div>
            )}
            {citizenAction !== null && showExtraInfo && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.actionLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{citizenAction}</div>
                </div>
            )}
            {parsedPassengers !== -1 && showExtraInfo && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.passengersLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{parsedPassengers}</div>
                </div>
            )}
            {parsedResources !== -1 && showExtraInfo && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.resourcesLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{formattedResources}</div>
                </div>
            )}
        </div>
    );

    // Check if the panel content would be empty
    const panelContent = renderPanelContent();
    const hasContent = React.Children.count(
        React.Children.toArray(panelContent.props.children).filter(Boolean)
    ) > 0;

    if (!hasContent) {
        return null;
    }

    return (
        <div style={{
            position: 'absolute',
            top: showCrosshair ? '60rem' : '10rem',
            right: '10rem',
            display: 'flex',
        }}>
            <div className="tool-options-panel_Se6">
                {panelContent}
            </div>
        </div>
    );
};

export default FollowedVehicleInfoPanel;
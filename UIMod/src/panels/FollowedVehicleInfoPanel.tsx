import React, { useRef, useEffect, useState } from 'react';
import { bindValue, useValue } from "cs2/api";

const UISettingsGroupOptions$ = bindValue<string>('fpc', 'UISettingsGroupOptions');
const FollowedEntityInfo$ = bindValue<string>('fpc', 'FollowedEntityInfo');
const ShowCrosshair$ = bindValue<boolean>('fpc', 'ShowCrosshair');

interface TranslationProps {
    nameLabel: string | null;
    speedLabel: string | null;
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

    const showInfoPanel = JSON.parse(uiSettingsGroupOptions).ShowInfoBox;
    const onlyShowSpeed = JSON.parse(uiSettingsGroupOptions).OnlyShowSpeed;
    const infoBoxSize = JSON.parse(uiSettingsGroupOptions).InfoBoxSize;

    const parsedSpeed = JSON.parse(followedEntityInfo).currentSpeed;
    const parsedUnits = JSON.parse(followedEntityInfo).unitsSystem;
    const parsedPassengers = JSON.parse(followedEntityInfo).passengers;
    const parsedResources = JSON.parse(followedEntityInfo).resources;
    const vehicleType = JSON.parse(followedEntityInfo).vehicleType;
    const citizenName = JSON.parse(followedEntityInfo).citizenName;
    const citizenAction = JSON.parse(followedEntityInfo).citizenAction;

    let formattedSpeed: string;

    if (parsedUnits == 1) {
        formattedSpeed = Math.round(parsedSpeed * 1.26) + " mph";
    } else {
        formattedSpeed = Math.round(parsedSpeed * 1.8) + " km/h";
    }

    let formattedResources = Math.round(parsedResources*100) + "%";

    useEffect(() => {
        const updateWidth = () => {
            if (speedDivRef.current) {
                const width = speedDivRef.current.offsetWidth;
                console.log("speedDivRef:", width);
                if (width > 0) {
                    setSpeedWidth(width / (window.innerWidth / 1920) + 40 + 5);
                }
                else {
                    requestAnimationFrame(updateWidth);
                }
            }
        };

        requestAnimationFrame(updateWidth);
    }, []);

    if (!showInfoPanel) {
        return null;
    }

    const infoBoxSizeClass = infoBoxSize === 0 ? 'small' :
        infoBoxSize === 2 ? 'large' : '';

    // Create content without rendering it yet
    const renderPanelContent = () => (
        <div className="item_bZY" ref={panelContentRef}>
            {citizenName !== null && !onlyShowSpeed && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.nameLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{citizenName}</div>
                </div>
            )}
            {parsedSpeed !== -1 && (
                <div
                    ref={speedDivRef}
                    className={`fpcc-info-group-speed-padding ${infoBoxSizeClass}`}
                    style={{ width: speedWidth ? `${speedWidth}rem` : 'auto' }}
                >
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.speedLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{formattedSpeed}</div>
                </div>
            )}
            {vehicleType !== null && !onlyShowSpeed && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.vehicleTypeLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{vehicleType}</div>
                </div>
            )}
            {citizenAction !== null && !onlyShowSpeed && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.actionLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{citizenAction}</div>
                </div>
            )}
            {parsedPassengers !== -1 && !onlyShowSpeed && (
                <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                    <div className={`fpcc-info-label ${infoBoxSizeClass}`}>{translation.passengersLabel}</div>
                    <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{parsedPassengers}</div>
                </div>
            )}
            {parsedResources !== -1 && !onlyShowSpeed && (
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
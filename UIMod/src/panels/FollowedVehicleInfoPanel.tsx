import React, { useRef, useEffect, useState } from 'react';
import { bindValue, useValue } from "cs2/api";

const UISettingsGroupOptions$ = bindValue<string>('fpc', 'UISettingsGroupOptions');
const FollowedEntityInfo$ = bindValue<string>('fpc', 'FollowedEntityInfo');
const ShowCrosshair$ = bindValue<boolean>('fpc', 'ShowCrosshair');

interface TranslationProps {
    nameLabel: string | null;
    speedLabel: string | null;
    vehicleTypeLabel: string | null;
    actionLabel: string | null;
    passengersLabel: string | null;
}

interface FollowedVehicleInfoPanelProps {
    translation: TranslationProps;
}

const FollowedVehicleInfoPanel: React.FC<FollowedVehicleInfoPanelProps> = ({ translation }) => {
    const speedDivRef = useRef<HTMLDivElement>(null);
    const [speedWidth, setSpeedWidth] = useState<number | undefined>(undefined);

    const uiSettingsGroupOptions = useValue(UISettingsGroupOptions$);
    const followedEntityInfo = useValue(FollowedEntityInfo$);
    const showCrosshair = useValue(ShowCrosshair$);

    // Get initial width on first render
    useEffect(() => {
        requestAnimationFrame(() => {
            if (speedDivRef.current) {
                const width = speedDivRef.current?.offsetWidth;
                if (width) {
                    setSpeedWidth(width / (window.innerWidth / 1920) + 40 + 5);
                }
            }

        });
    }, []);

    const showInfoPanel: boolean = JSON.parse(uiSettingsGroupOptions).ShowInfoBox;
    const onlyShowSpeed: boolean = JSON.parse(uiSettingsGroupOptions).OnlyShowSpeed;
    const infoBoxSize: number = JSON.parse(uiSettingsGroupOptions).InfoBoxSize;

    const parsedSpeed: number = JSON.parse(followedEntityInfo).currentSpeed;
    const parsedUnits: number = JSON.parse(followedEntityInfo).unitsSystem;
    const parsedPassengers: number = JSON.parse(followedEntityInfo).passengers;
    const vehicleType: string = JSON.parse(followedEntityInfo).vehicleType;
    const citizenName: string = JSON.parse(followedEntityInfo).citizenName;
    const citizenAction: string = JSON.parse(followedEntityInfo).citizenAction;

    let formattedSpeed: string;
    if (parsedUnits == 1) {
        formattedSpeed = Math.round(parsedSpeed * 1.26) + " mph";
    } else {
        formattedSpeed = Math.round(parsedSpeed * 1.8) + " km/h";
    }

    if (!showInfoPanel) {
        return null;
    }

    const infoBoxSizeClass = {
        0: 'small',
        2: 'large'
    }[infoBoxSize] || '';

    return (
        <div style={{
            position: 'absolute',
            top: showCrosshair ? '60rem' : '10rem',
            right: '10rem',
            display: 'flex',
        }}>
            {/*85+60*/}
            <div className="tool-options-panel_Se6">
                <div className="item_bZY">
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
                </div>
            </div>
        </div>
    );
};

export default FollowedVehicleInfoPanel;
import { bindValue, useValue } from "cs2/api";
import { useEffect } from "react";

const UISettingsGroupOptions$ = bindValue<string>('fpc', 'UISettingsGroupOptions');
const FollowedEntityInfo$ = bindValue<string>('fpc', 'FollowedEntityInfo');

const ShowCrosshair$ = bindValue<boolean>('fpc', 'ShowCrosshair');


const FollowedVehicleInfoPanel: React.FC = () => {

    const uiSettingsGroupOptions = useValue(UISettingsGroupOptions$);

    useEffect(() => {
        console.log(uiSettingsGroupOptions);
    }, [uiSettingsGroupOptions]);

    const followedEntityInfo = useValue(FollowedEntityInfo$);

    const showCrosshair = useValue(ShowCrosshair$);

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
    }
    else {
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
            top: showCrosshair ? '60rem': '10rem',
            right: '10rem',
            display: 'flex',
        }}>
        {/*85+60*/}
        <div className="tool-options-panel_Se6">
                <div className="item_bZY">
                    {citizenName !== null && !onlyShowSpeed && (
                        <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                            <div className={`fpcc-info-label ${infoBoxSizeClass}`}>Name</div>
                            <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{citizenName}</div>
                        </div>
                    )}
                    {parsedSpeed !== -1 && (
                        <div className={`fpcc-speed-width ${infoBoxSizeClass}`}>
                            <div className={`fpcc-info-label ${infoBoxSizeClass}`}>Speed</div>
                            <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{formattedSpeed}</div>
                        </div>
                    )}
                    {vehicleType !== null && !onlyShowSpeed && (
                        <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                            <div className={`fpcc-info-label ${infoBoxSizeClass}`}>Vehicle Type</div>
                            <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{vehicleType}</div>
                        </div>
                    )}
                    {citizenAction !== null && !onlyShowSpeed && (
                        <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                            <div className={`fpcc-info-label ${infoBoxSizeClass}`}>Action</div>
                            <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{citizenAction}</div>
                        </div>
                    )}
                    {parsedPassengers !== -1 && !onlyShowSpeed && (
                        <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                            <div className={`fpcc-info-label ${infoBoxSizeClass}`}>Passengers</div>
                            <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{parsedPassengers}</div>
                        </div>
                )}
                </div>
            </div>
        </div>
    );
};

export default FollowedVehicleInfoPanel;
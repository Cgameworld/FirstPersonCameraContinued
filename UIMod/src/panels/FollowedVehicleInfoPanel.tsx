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
    const showVehicleType: boolean = JSON.parse(uiSettingsGroupOptions).ShowVehicleType;
    const infoBoxSize: number = JSON.parse(uiSettingsGroupOptions).InfoBoxSize;

    const parsedSpeed: number = JSON.parse(followedEntityInfo).currentSpeed;
    const parsedUnits: number = JSON.parse(followedEntityInfo).unitsSystem;
    const parsedPassengers: number = JSON.parse(followedEntityInfo).passengers;
    const vehicleType: string = JSON.parse(followedEntityInfo).vehicleType;

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

    const infoBoxSizeClass = infoBoxSize === 1 ? 'large' : '';

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
                    {parsedSpeed !== -1 && (
                        <div className={`fpcc-speed-width`}>
                            <div className={`fpcc-info-label ${infoBoxSizeClass}`}>Speed</div>
                            <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{formattedSpeed}</div>
                        </div>
                    )}
                    {vehicleType !== null && showVehicleType && (
                        <div className={`fpcc-info-group ${infoBoxSizeClass}`}>
                            <div className={`fpcc-info-label ${infoBoxSizeClass}`}>Vehicle Type</div>
                            <div className={`fpcc-info-data ${infoBoxSizeClass}`}>{vehicleType}</div>
                        </div>
                    )}
                    {parsedPassengers !== -1 && (
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
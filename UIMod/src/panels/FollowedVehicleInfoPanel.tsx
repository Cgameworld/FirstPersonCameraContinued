import { bindValue, useValue } from "cs2/api";

const FollowedEntityInfo$ = bindValue<string>('fpc', 'FollowedEntityInfo');
const ShowCrosshair$ = bindValue<boolean>('fpc', 'ShowCrosshair');


const FollowedVehicleInfoPanel: React.FC = () => {

    const followedEntityInfo = useValue(FollowedEntityInfo$);
    const showCrosshair = useValue(ShowCrosshair$);

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
                        <div style={{ width: '145rem' }}>  
                        <div className="fpcc-info-label">Speed</div>
                        <div className="fpcc-info-data">{formattedSpeed}</div>
                    </div>
                    )}
                {vehicleType !== null && (
                    <div className="fpcc-info-group">
                        <div className="fpcc-info-label">Vehicle Type</div>
                        <div className="fpcc-info-data">{vehicleType}</div>
                    </div>
                )}
                {parsedPassengers !== -1 && (
                    <div className="fpcc-info-group">
                        <div className="fpcc-info-label">Passengers</div>
                        <div className="fpcc-info-data">{parsedPassengers}</div>
                    </div>
                )}
                </div>
            </div>
        </div>
    );
};

export default FollowedVehicleInfoPanel;
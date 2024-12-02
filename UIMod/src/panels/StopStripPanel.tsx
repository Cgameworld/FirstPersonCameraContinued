import React from 'react';
import 'style/StopStripPanel.scss';

const StopStripPanel: React.FC = () => {
    const stations = [
        { name: 'Cedar Park' },
        { name: 'Cedar Park' },
        { name: 'Cedar Park' },
        { name: 'Cedar Park' },
        { name: 'Cedar Park' },
        { name: 'Cedar Park' }
    ];

    return (
        <div className="fpcc-stopstrip-container">
            <div className="fpcc-stopstrip-progress-track">
                <div className="fpcc-stopstrip-progress-bar-container">
                    <div className="fpcc-stopstrip-progress-bar"></div>
                </div>
                {stations.map((station, index) => (
                    <div key={index} className="fpcc-stopstrip-station">
                        <div className="fpcc-stopstrip-station-dot"></div>
                        <div className="fpcc-stopstrip-station-name">{station.name}</div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default StopStripPanel;
import React from 'react';
import { bindValue, trigger, useValue } from "cs2/api";
import engine from 'cohtml/cohtml';

const NoEntitiesError$ = bindValue<string>('fpc', 'NoEntitiesError', '');

const ErrorPopup: React.FC = () => {
    const errorMessage = useValue(NoEntitiesError$);

    if (!errorMessage) return null;

    const onContinue = () => {
        engine.trigger("audio.playSound", "select-item", 1);
        trigger("fpc", "DismissNoEntitiesError");
    };

    return (
        <div
            style={{
                position: "fixed",
                top: 0,
                left: 0,
                width: "100%",
                height: "100%",
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                zIndex: 99999,
                backgroundColor: "rgba(0, 0, 0, 0.5)",
            }}
        >
            <div className="panel_YqS error-dialog_iaV" style={{ marginTop:'-150rem', width:'650rem' }}>
                <div className="header_jAe header_Bpo child-opacity-transition_nkS">
                    <div className="title-bar_PF4">
                        <div className="icon_VQU">
                            <img className="iconImg_ThV" src="coui://uil/Standard/VideoCamera.svg" />
                        </div>
                        <div className="icon-space_h_f"></div>
                        <div className="title_ctd title_zQN">No Entities Found</div>
                    </div>
                </div>
                <div className="content_VBF content_AD7 child-opacity-transition_nkS">
                    <div className="icon-layout_cZT row_L6K">
                        <img className="icon_JQd" src="Media/Misc/Warning.svg" />
                        <div className="main-column_Jzk">
                            <div className="error-message_r4_" style={{ maxHeight: '540rem' }}>
                                <div className="paragraphs_nbD">
                                    <p className="p_CKq" cohinline="cohinline">
                                        {errorMessage}
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="buttons_lZi row_L6K">
                        <button className="button_HeP selected button_gJo error-button_M8i" onClick={onContinue}>
                            OK
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ErrorPopup;

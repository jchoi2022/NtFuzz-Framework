VBoxManage clonevm win10-fuzzer --name %1 --basefolder $2 --options keepdisknames --options keephwuuids --register
VBoxManage modifyvm %1 --nic1 none
VBoxManage sharedfolder add %1 --name task --hostpath %cd%\tasks\%1 --automount
